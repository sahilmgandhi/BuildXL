// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics.ContractsLight;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Distributed.NuCache;
using BuildXL.Cache.ContentStore.Distributed.Sessions;
using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.Interfaces.Distributed;
using BuildXL.Cache.ContentStore.Interfaces.FileSystem;
using BuildXL.Cache.ContentStore.Interfaces.Results;
using BuildXL.Cache.ContentStore.Interfaces.Sessions;
using BuildXL.Cache.ContentStore.Interfaces.Stores;
using BuildXL.Cache.ContentStore.Interfaces.Time;
using BuildXL.Cache.ContentStore.Interfaces.Tracing;
using BuildXL.Cache.ContentStore.Stores;
using BuildXL.Cache.ContentStore.Tracing;
using BuildXL.Cache.ContentStore.Tracing.Internal;
using BuildXL.Cache.ContentStore.Utils;
using BuildXL.Utilities.Tasks;

namespace BuildXL.Cache.ContentStore.Distributed.Stores
{
    /// <summary>
    /// A store that is based on content locations for opaque file locations.
    /// </summary>
    /// <typeparam name="T">The content locations being stored.</typeparam>
    public class DistributedContentStore<T> : StartupShutdownBase, IContentStore, IRepairStore, IDistributedLocationStore, IStreamStore, ICopyRequestHandler, IPushFileHandler, IDeleteFileHandler
        where T : PathBase
    {
        /// <summary>
        /// The location of the local cache root
        /// </summary>
        public MachineLocation LocalMachineLocation { get; }

        private readonly IContentLocationStoreFactory _contentLocationStoreFactory;
        private readonly ContentStoreTracer _tracer = new ContentStoreTracer(nameof(DistributedContentStore<T>));
        private readonly NagleQueue<ContentHash> _evictionNagleQueue;
        private NagleQueue<ContentHashWithSize> _touchNagleQueue;
        private readonly ContentTrackerUpdater _contentTrackerUpdater;
        private readonly bool _enableDistributedEviction;
        private readonly PinCache _pinCache;
        private readonly IClock _clock;

        /// <summary>
        /// Flag for testing using local Redis instance.
        /// </summary>
        internal bool DisposeContentStoreFactory = true;

        internal IContentStore InnerContentStore { get; }

        /// <inheritdoc />
        protected override Tracer Tracer => _tracer;

        private IContentLocationStore _contentLocationStore;

        private readonly DistributedContentStoreSettings _settings;

        /// <summary>
        /// Task source that is set to completion state when the system is fully initialized.
        /// The main goal of this field is to avoid the race condition when eviction is triggered during startup
        /// when hibernated sessions are not fully reloaded.
        /// </summary>
        private readonly TaskSourceSlim<BoolResult> _postInitializationCompletion = TaskSourceSlim.Create<BoolResult>();

        private DistributedContentCopier<T> _distributedCopier;
        private readonly Func<IContentLocationStore, DistributedContentCopier<T>> _distributedCopierFactory;
        internal Lazy<Task<Result<ReadOnlyDistributedContentSession<T>>>> ProactiveCopySession;

        /// <nodoc />
        public DistributedContentStore(
            byte[] localMachineLocation,
            Func<NagleQueue<ContentHash>, DistributedEvictionSettings, ContentStoreSettings, TrimBulkAsync, IContentStore> innerContentStoreFunc,
            IContentLocationStoreFactory contentLocationStoreFactory,
            IFileExistenceChecker<T> fileExistenceChecker,
            IFileCopier<T> fileCopier,
            IPathTransformer<T> pathTransform,
            IContentCommunicationManager copyRequester,
            AbsolutePath tempFolderForCopies,
            IAbsFileSystem fileSystem,
            DistributedContentStoreSettings settings,
            IClock clock = null,
            ContentStoreSettings contentStoreSettings = null)
        {
            Contract.Requires(settings != null);

            LocalMachineLocation = new MachineLocation(localMachineLocation);
            _contentLocationStoreFactory = contentLocationStoreFactory;
            _clock = clock;

            contentStoreSettings = contentStoreSettings ?? ContentStoreSettings.DefaultSettings;
            _settings = settings;

            // Queue is created in unstarted state because the eviction function
            // requires the context passed at startup.
            _evictionNagleQueue = NagleQueue<ContentHash>.CreateUnstarted(
                Redis.RedisContentLocationStoreConstants.BatchDegreeOfParallelism,
                Redis.RedisContentLocationStoreConstants.BatchInterval,
                _settings.LocationStoreBatchSize);

            _distributedCopierFactory = (contentLocationStore) =>
            {
                return new DistributedContentCopier<T>(
                    tempFolderForCopies,
                    _settings,
                    fileSystem,
                    fileCopier,
                    fileExistenceChecker,
                    copyRequester,
                    pathTransform,
                    _clock,
                    contentLocationStore);
            };

            _enableDistributedEviction = _settings.ReplicaCreditInMinutes != null;
            var distributedEvictionSettings = _enableDistributedEviction ? SetUpDistributedEviction(_settings.ReplicaCreditInMinutes, _settings.LocationStoreBatchSize) : null;

            var enableTouch = _settings.ContentHashBumpTime.HasValue;
            if (enableTouch)
            {
                _contentTrackerUpdater = new ContentTrackerUpdater(ScheduleBulkTouch, _settings.ContentHashBumpTime.Value, clock: _clock);
            }

            TrimBulkAsync trimBulkAsync = null;
            InnerContentStore = innerContentStoreFunc(_evictionNagleQueue, distributedEvictionSettings, contentStoreSettings, trimBulkAsync);

            if (settings.PinConfiguration?.IsPinCachingEnabled == true)
            {
                _pinCache = new PinCache(clock: _clock);
            }
        }

        private Task<Result<ReadOnlyDistributedContentSession<T>>> CreateCopySession(Context context)
        {
            var sessionId = Guid.NewGuid();
            var operationContext = OperationContext(new Context(context, sessionId));
            return operationContext.PerformOperationAsync(_tracer,
                async () =>
                {
                    // NOTE: We use ImplicitPin.None so that the OpenStream calls triggered by RequestCopy will only pull the content, NOT pin it in the local store.
                    var sessionResult = CreateReadOnlySession(operationContext, $"{sessionId}-DefaultCopy", ImplicitPin.None).ThrowIfFailure();
                    var session = sessionResult.Session;

                    await session.StartupAsync(context).ThrowIfFailure();
                    return Result.Success(session as ReadOnlyDistributedContentSession<T>);
                });
        }

        /// <inheritdoc />
        public override Task<BoolResult> StartupAsync(Context context)
        {
            var startupTask = base.StartupAsync(context);

            ProactiveCopySession = new Lazy<Task<Result<ReadOnlyDistributedContentSession<T>>>>(() => CreateCopySession(context));

            if (_settings.SetPostInitializationCompletionAfterStartup)
            {
                context.Debug("Linking post-initialization completion task with the result of StartupAsync.");
                _postInitializationCompletion.LinkToTask(startupTask);
            }

            return startupTask;
        }

        /// <inheritdoc />
        public void PostInitializationCompleted(Context context, BoolResult result)
        {
            context.Debug($"Setting result for post-initialization completion task to '{result}'.");
            _postInitializationCompletion.TrySetResult(result);
        }

        /// <inheritdoc />
        protected override async Task<BoolResult> StartupCoreAsync(OperationContext context)
        {
            // NOTE: We create and start the content location store before the inner content store just in case the
            // inner content store starts background eviction after startup. We need the content store to be initialized
            // so that it can be queried and used to unregister content.
            await _contentLocationStoreFactory.StartupAsync(context).ThrowIfFailure();

            _contentLocationStore = await _contentLocationStoreFactory.CreateAsync(LocalMachineLocation);

            _distributedCopier = _distributedCopierFactory(_contentLocationStore);
            await _distributedCopier.StartupAsync(context).ThrowIfFailure();

            if (_contentLocationStore is TransitioningContentLocationStore tcs && tcs.IsLocalLocationStoreEnabled)
            {
                // Define proactive copy logic.
                async Task<ProactiveCopyResult> proactiveCopyTaskFactory(OperationContext operationContext, ContentHash hash)
                {
                    var sessionResult = await ProactiveCopySession.Value;
                    if (sessionResult)
                    {
                        return await sessionResult.Value.ProactiveCopyIfNeededAsync(operationContext, hash, tryBuildRing: false);
                    }

                    return new ProactiveCopyResult(new BoolResult("Failed to retrieve session for proactive copies."));
                }

                tcs.LocalLocationStore.PreStartupInitialize(context, InnerContentStore as ILocalContentStore, _distributedCopier, proactiveCopyTaskFactory);
            }

            // Initializing inner store before initializing LocalLocationStore because
            // LocalLocationStore may use inner store for reconciliation purposes
            await InnerContentStore.StartupAsync(context).ThrowIfFailure();

            await _contentLocationStore.StartupAsync(context).ThrowIfFailure();

            Func<ContentHash[], Task> evictionHandler;
            var localContext = context.CreateNested();
            if (_enableDistributedEviction)
            {
                evictionHandler = hashes => EvictContentAsync(localContext, hashes);
            }
            else
            {
                evictionHandler = hashes => DistributedGarbageCollectionAsync(localContext, hashes);
            }

            // Queue is created in unstarted state because the eviction function
            // requires the context passed at startup. So we start the queue here.
            _evictionNagleQueue.Start(evictionHandler);

            var touchContext = context.CreateNested();
            _touchNagleQueue = NagleQueue<ContentHashWithSize>.Create(
                hashes => TouchBulkAsync(touchContext, hashes),
                Redis.RedisContentLocationStoreConstants.BatchDegreeOfParallelism,
                Redis.RedisContentLocationStoreConstants.BatchInterval,
                batchSize: _settings.LocationStoreBatchSize);

            return BoolResult.Success;
        }

        /// <inheritdoc />
        protected override async Task<BoolResult> ShutdownCoreAsync(OperationContext context)
        {
            var results = new List<Tuple<string, BoolResult>>();

            if (ProactiveCopySession?.IsValueCreated == true)
            {
                var sessionResult = await ProactiveCopySession.Value;
                if (sessionResult.Succeeded)
                {
                    var proactiveCopySessionShutdownResult = await sessionResult.Value.ShutdownAsync(context);
                    results.Add(Tuple.Create(nameof(ProactiveCopySession), proactiveCopySessionShutdownResult));
                }
            }

            var innerResult = await InnerContentStore.ShutdownAsync(context);
            results.Add(Tuple.Create(nameof(InnerContentStore), innerResult));

            _evictionNagleQueue?.Dispose();
            _touchNagleQueue?.Dispose();

            if (_contentLocationStore != null)
            {
                var locationStoreResult = await _contentLocationStore.ShutdownAsync(context);
                results.Add(Tuple.Create(nameof(_contentLocationStore), locationStoreResult));
            }

            var factoryResult = await _contentLocationStoreFactory.ShutdownAsync(context);
            results.Add(Tuple.Create(nameof(_contentLocationStoreFactory), factoryResult));

            if (_distributedCopier != null)
            {
                var copierResult = await _distributedCopier.ShutdownAsync(context);
                results.Add(Tuple.Create(nameof(_distributedCopier), copierResult));
            }

            return ShutdownErrorCompiler(results);
        }

        private void ScheduleBulkTouch(List<ContentHashWithSize> content)
        {
            Contract.Assert(_touchNagleQueue != null);
            _touchNagleQueue.EnqueueAll(content);
        }

        /// <summary>
        /// Batch content hashes that were not removed during eviction to re-register with the content tracker.
        /// </summary>
        private async Task EvictContentAsync(Context context, ContentHash[] contentHashes)
        {
            var contentHashesAndLocations = new List<ContentHashWithSizeAndLocations>();
            foreach (ContentHash contentHash in contentHashes)
            {
                _tracer.Debug(context, $"[DistributedEviction] Re-adding local location for content hash {contentHash.ToShortString()} because it was not evicted");
                contentHashesAndLocations.Add(new ContentHashWithSizeAndLocations(contentHash));
            }

            // LocationStoreOption.None tells the content tracker to:
            //      1) Only update the location if the hash exists
            //      2) Not update the expiry
            var result = await _contentLocationStore.UpdateBulkAsync(
                context, contentHashesAndLocations, CancellationToken.None, UrgencyHint.Low, LocationStoreOption.None);

            if (!result.Succeeded)
            {
                _tracer.Error(context, $"[DistributedEviction] Unable to re-add content hashes to Redis. errorMessage=[{result.ErrorMessage}] diagnostics=[{result.Diagnostics}]");
            }
        }

        private async Task DistributedGarbageCollectionAsync(Context context, ContentHash[] contentHashes)
        {
            var result = await UnregisterAsync(context, contentHashes, CancellationToken.None);
            if (!result.Succeeded)
            {
                _tracer.Error(context, $"[GarbageCollection] Unable to remove evicted content hashes from Redis. errorMessage=[{result.ErrorMessage}] diagnostics=[{result.Diagnostics}]");
            }
        }

        private async Task TouchBulkAsync(Context context, ContentHashWithSize[] contentHashesWithSize)
        {
            var result = await _contentLocationStore.TouchBulkAsync(context, contentHashesWithSize, CancellationToken.None, UrgencyHint.Low);
            if (!result.Succeeded)
            {
                _tracer.Error(context, $"Unable to touch {contentHashesWithSize.Length} hashes in the content tracker. errorMessage=[{result.ErrorMessage}] diagnostics=[{result.Diagnostics}]");
            }
        }

        /// <inheritdoc />
        public CreateSessionResult<IReadOnlyContentSession> CreateReadOnlySession(Context context, string name, ImplicitPin implicitPin)
        {
            return CreateReadOnlySessionCall.Run(_tracer, OperationContext(context), name, () =>
            {
                CreateSessionResult<IContentSession> innerSessionResult = InnerContentStore.CreateSession(context, name, implicitPin);

                if (innerSessionResult.Succeeded)
                {
                    var session = new ReadOnlyDistributedContentSession<T>(
                            name,
                            innerSessionResult.Session,
                            _contentLocationStore,
                            _distributedCopier,
                            LocalMachineLocation,
                            pinCache: _pinCache,
                            contentTrackerUpdater: _contentTrackerUpdater,
                            settings: _settings);
                    return new CreateSessionResult<IReadOnlyContentSession>(session);
                }

            return new CreateSessionResult<IReadOnlyContentSession>(innerSessionResult, "Could not initialize inner content session with error");
            });
        }

        /// <inheritdoc />
        public CreateSessionResult<IContentSession> CreateSession(Context context, string name, ImplicitPin implicitPin)
        {
            return CreateSessionCall.Run(_tracer, OperationContext(context), name, () =>
            {
                CreateSessionResult<IContentSession> innerSessionResult = InnerContentStore.CreateSession(context, name, implicitPin);

                if (innerSessionResult.Succeeded)
                {
                    var session = new DistributedContentSession<T>(
                            name,
                            innerSessionResult.Session,
                            _contentLocationStore,
                            _distributedCopier,
                            LocalMachineLocation,
                            pinCache: _pinCache,
                            contentTrackerUpdater: _contentTrackerUpdater,
                            settings: _settings);
                    return new CreateSessionResult<IContentSession>(session);
                }

                return new CreateSessionResult<IContentSession>(innerSessionResult, "Could not initialize inner content session with error");
            });
        }

        /// <inheritdoc />
        public Task<GetStatsResult> GetStatsAsync(Context context)
        {
            return GetStatsCall<ContentStoreTracer>.RunAsync(_tracer, OperationContext(context), async () =>
            {
                var result = await InnerContentStore.GetStatsAsync(context);
                if (result.Succeeded)
                {
                    var counterSet = result.CounterSet;
                    if (_contentLocationStore != null)
                    {
                        var contentLocationStoreCounters = _contentLocationStore.GetCounters(context);
                        counterSet.Merge(contentLocationStoreCounters, "ContentLocationStore.");
                    }

                    if (_pinCache != null)
                    {
                        counterSet.Merge(_pinCache.GetCounters(context), "PinCache.");
                    }

                    return new GetStatsResult(counterSet);
                }

                return result;
            });
        }

        /// <summary>
        /// Remove local location from the content tracker.
        /// </summary>
        public async Task<StructResult<long>> RemoveFromTrackerAsync(Context context)
        {
            if (_settings.EnableRepairHandling && InnerContentStore is ILocalContentStore localStore)
            {
                var result = await _contentLocationStore.InvalidateLocalMachineAsync(context, localStore, CancellationToken.None);
                if (!result)
                {
                    return new StructResult<long>(result);
                }
            }

            // New logic doesn't have the content removed count
            return StructResult.Create((long)0);
        }

        /// <summary>
        /// Determines if final BoolResult is success or error.
        /// </summary>
        /// <param name="results">Paired List of shutdowns and their results.</param>
        /// <returns>BoolResult as success or error. If error, error message lists messages in order they occurred.</returns>
        private static BoolResult ShutdownErrorCompiler(IReadOnlyList<Tuple<string, BoolResult>> results)
        {
            var sb = new StringBuilder();
            foreach (Tuple<string, BoolResult> result in results)
            {
                if (!result.Item2.Succeeded)
                {
                    // TODO: Consider compiling Item2's Diagnostics into the final result's Diagnostics instead of ErrorMessage (bug 1365340)
                    sb.Append(result.Item1 + ": " + result.Item2 + " ");
                }
            }

            return sb.Length != 0 ? new BoolResult(sb.ToString()) : BoolResult.Success;
        }

        /// <nodoc />
        protected override void DisposeCore()
        {
            InnerContentStore.Dispose();

            if (DisposeContentStoreFactory)
            {
                _contentLocationStoreFactory.Dispose();
            }
        }

        private DistributedEvictionSettings SetUpDistributedEviction(int? replicaCreditInMinutes, int locationStoreBatchSize)
        {
            Contract.Assert(_enableDistributedEviction);

            return new DistributedEvictionSettings(
                (context, contentHashesWithInfo, cts, urgencyHint) =>
                    _contentLocationStore.TrimOrGetLastAccessTimeAsync(context, contentHashesWithInfo, cts, urgencyHint),
                locationStoreBatchSize,
                replicaCreditInMinutes,
                this);
        }

        /// <nodoc />
        public bool CanComputeLru => (_contentLocationStore as IDistributedLocationStore)?.CanComputeLru ?? false;

        /// <nodoc />
        public Task<BoolResult> UnregisterAsync(Context context, IReadOnlyList<ContentHash> contentHashes, CancellationToken token)
        {
            if (InnerContentStore is ILocalContentStore localContentStore)
            {
                // Filter out hashes which exist in the local content store (may have been re-added by a recent put).
                var filteredHashes = contentHashes.Where(hash => !localContentStore.Contains(hash)).ToList();
                if (filteredHashes.Count != contentHashes.Count)
                {
                    Tracer.OperationDebug(context, $"Hashes not unregistered because they are still present in local store: [{string.Join(",", contentHashes.Except(filteredHashes))}]");
                    contentHashes = filteredHashes;
                }
            }

            return _contentLocationStore.TrimBulkAsync(context, contentHashes, token, UrgencyHint.Nominal);
        }

        /// <nodoc />
        public IEnumerable<ContentEvictionInfo> GetHashesInEvictionOrder(Context context, IReadOnlyList<ContentHashWithLastAccessTimeAndReplicaCount> contentHashesWithInfo)
        {
            // Ensure startup was called then wait for it to complete successfully (or error)
            // This logic is important to avoid runtime errors when, for instance, QuotaKeeper tries
            // to evict content right after startup and calls GetLruPages.
            Contract.Assert(StartupStarted);
            WaitForPostInitializationCompletionIfNeeded(context);

            Contract.Assert(_contentLocationStore is IDistributedLocationStore);
            if (_contentLocationStore is IDistributedLocationStore distributedStore)
            {
                return distributedStore.GetHashesInEvictionOrder(context, contentHashesWithInfo);
            }
            else
            {
                throw Contract.AssertFailure($"Cannot call GetLruPages when CanComputeLru returns false");
            }
        }

        private void WaitForPostInitializationCompletionIfNeeded(Context context)
        {
            var task = _postInitializationCompletion.Task;
            if (!task.IsCompleted)
            {
                var operationContext = new OperationContext(context);
                operationContext.PerformOperation(Tracer, () => waitForCompletion(), traceOperationStarted: false).ThrowIfFailure();
            }

            BoolResult waitForCompletion()
            {
                context.Debug($"Post-initialization is not done. Waiting for it to finish...");
                return task.GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Attempts to get local location store if enabled
        /// </summary>
        public bool TryGetLocalLocationStore(out LocalLocationStore localLocationStore)
        {
            if (_contentLocationStore is TransitioningContentLocationStore tcs
                && tcs.IsLocalLocationStoreEnabled)
            {
                localLocationStore = tcs.LocalLocationStore;
                return true;
            }

            localLocationStore = null;
            return false;
        }

        /// <summary>
        /// Gets the associated local location store instance
        /// </summary>
        public LocalLocationStore LocalLocationStore => (_contentLocationStore as TransitioningContentLocationStore)?.LocalLocationStore;

        /// <summary>
        /// Checks the LLS <see cref="DistributedCentralStorage"/> for the content if available and returns
        /// the storage instance if content is found
        /// </summary>
        private bool CheckLlsForContent(ContentHash desiredContent, out DistributedCentralStorage storage)
        {
            if (_contentLocationStore is TransitioningContentLocationStore tcs
                && tcs.IsLocalLocationStoreEnabled
                && tcs.LocalLocationStore.DistributedCentralStorage != null
                && tcs.LocalLocationStore.DistributedCentralStorage.HasContent(desiredContent))
            {
                storage = tcs.LocalLocationStore.DistributedCentralStorage;
                return true;
            }

            storage = default;
            return false;
        }

        /// <inheritdoc />
        public async Task<OpenStreamResult> StreamContentAsync(Context context, ContentHash contentHash)
        {
            // NOTE: Checking LLS for content needs to happen first since the query to the inner stream store result
            // is used even if the result is fails.
            if (CheckLlsForContent(contentHash, out var storage))
            {
                var result = await storage.StreamContentAsync(context, contentHash);
                if (result.Succeeded)
                {
                    return result;
                }
            }

            if (InnerContentStore is IStreamStore innerStreamStore)
            {
                return await innerStreamStore.StreamContentAsync(context, contentHash);
            }

            return new OpenStreamResult($"{InnerContentStore} does not implement {nameof(IStreamStore)} in {nameof(DistributedContentStore<T>)}.");
        }

        /// <inheritdoc />
        public async Task<FileExistenceResult> CheckFileExistsAsync(Context context, ContentHash contentHash)
        {
            // NOTE: Checking LLS for content needs to happen first since the query to the inner stream store result
            // is used even if the result is fails.
            if (CheckLlsForContent(contentHash, out var storage))
            {
                return new FileExistenceResult(FileExistenceResult.ResultCode.FileExists);
            }

            if (InnerContentStore is IStreamStore innerStreamStore)
            {
                return await innerStreamStore.CheckFileExistsAsync(context, contentHash);
            }

            return new FileExistenceResult(FileExistenceResult.ResultCode.Error, $"{InnerContentStore} does not implement {nameof(IStreamStore)} in {nameof(DistributedContentStore<T>)}.");
        }

        Task<DeleteResult> IDeleteFileHandler.HandleDeleteAsync(Context context, ContentHash contentHash, DeleteContentOptions deleteOptions) => DeleteAsync(context, contentHash, deleteOptions);

        /// <inheritdoc />
        public Task<DeleteResult> DeleteAsync(Context context, ContentHash contentHash, DeleteContentOptions deleteOptions)
        {
            var operationContext = OperationContext(context);
            deleteOptions ??= new DeleteContentOptions() {DeleteLocalOnly = true};

            return operationContext.PerformOperationAsync(Tracer,
                async () =>
                {
                    var deleteResult = await InnerContentStore.DeleteAsync(context, contentHash, deleteOptions);
                    var contentHashes = new ContentHash[] { contentHash };
                    if (!deleteResult)
                    {
                        return deleteResult;
                    }

                    // Tell the event hub that this machine has removed the content locally
                    var unRegisterResult = await UnregisterAsync(context, contentHashes, operationContext.Token).ThrowIfFailure();
                    if (!unRegisterResult)
                    {
                        return new DeleteResult(unRegisterResult, unRegisterResult.ToString());
                    }

                    if (deleteOptions.DeleteLocalOnly)
                    {
                        return deleteResult;
                    }

                    var result = await _contentLocationStore.GetBulkAsync(context, contentHashes, operationContext.Token, UrgencyHint.Nominal, GetBulkOrigin.Local);
                    if (!result)
                    {
                        return new DeleteResult(result, result.ToString());
                    }

                    // Go through each machine that has this content, and delete async locally on each machine.
                    if (result.ContentHashesInfo[0].Locations != null)
                    {
                        var machineLocations = result.ContentHashesInfo[0].Locations;
                        return await _distributedCopier.DeleteAsync(operationContext, contentHash, machineLocations);
                    }

                    return deleteResult;
                });
        }

        /// <inheritdoc />
        public Task<BoolResult> HandleCopyFileRequestAsync(Context context, ContentHash hash)
        {
            var operationContext = OperationContext(context);
            return operationContext.PerformOperationAsync(Tracer,
                async () =>
                {
                    var session = await ProactiveCopySession.Value.ThrowIfFailureAsync();
                    using (await session.OpenStreamAsync(context, hash, operationContext.Token).ThrowIfFailureAsync(o => o.Stream))
                    {
                        // Opening stream to ensure the content is copied locally. Stream is immediately disposed.
                    }

                    return BoolResult.Success;
                },
                traceOperationStarted: false,
                extraEndMessage: _ => $"Hash=[{hash.ToShortString()}]");
        }

        /// <inheritdoc />
        public async Task<PutResult> HandlePushFileAsync(Context context, ContentHash hash, AbsolutePath sourcePath, CancellationToken token)
        {
            if (InnerContentStore is IPushFileHandler inner)
            {
                var result = await inner.HandlePushFileAsync(context, hash, sourcePath, token);
                if (!result)
                {
                    return result;
                }

                var registerResult = await _contentLocationStore.RegisterLocalLocationAsync(context, new[] { new ContentHashWithSize(hash, result.ContentSize) }, token, UrgencyHint.Nominal, touch: false);
                if (!registerResult)
                {
                    return new PutResult(registerResult);
                }

                return result;
            }

            return new PutResult(new InvalidOperationException($"{nameof(InnerContentStore)} does not implement {nameof(IPushFileHandler)}"), hash);
        }

        /// <inheritdoc />
        public bool HasContentLocally(Context context, ContentHash hash)
        {
            if (InnerContentStore is IPushFileHandler inner)
            {
                return inner.HasContentLocally(context, hash);
            }

            return false;
        }
    }
}
