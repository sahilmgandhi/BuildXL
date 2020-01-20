﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Linq;
using BuildXL.Engine;
using BuildXL.Pips;
using BuildXL.Pips.Graph;
using BuildXL.Pips.Operations;
using BuildXL.Scheduler.Tracing;
using BuildXL.Utilities;
using BuildXL.Utilities.Collections;
using BuildXL.Xldb.Proto;
using static BuildXL.Utilities.HierarchicalNameTable;
using AbsolutePath = BuildXL.Utilities.AbsolutePath;
using ContentHash = BuildXL.Cache.ContentStore.Hashing.ContentHash;
using CopyFile = BuildXL.Pips.Operations.CopyFile;
using DirectoryArtifact = BuildXL.Utilities.DirectoryArtifact;
using EnvironmentVariable = BuildXL.Xldb.Proto.EnvironmentVariable;
using FileArtifact = BuildXL.Utilities.FileArtifact;
using FileOrDirectoryArtifact = BuildXL.Utilities.FileOrDirectoryArtifact;
using Fingerprint = BuildXL.Cache.MemoizationStore.Interfaces.Sessions.Fingerprint;
using IpcClientInfo = BuildXL.Xldb.Proto.IpcClientInfo;
using IpcPip = BuildXL.Pips.Operations.IpcPip;
using MountPathExpander = BuildXL.Engine.MountPathExpander;
using ObservedPathEntry = BuildXL.Scheduler.Fingerprints.ObservedPathEntry;
using ObservedPathSet = BuildXL.Scheduler.Fingerprints.ObservedPathSet;
using Pip = BuildXL.Pips.Operations.Pip;
using PipGraph = BuildXL.Pips.Graph.PipGraph;
using PipProvenance = BuildXL.Pips.Operations.PipProvenance;
using PipType = BuildXL.Pips.Operations.PipType;
using ProcessPipExecutionPerformance = BuildXL.Pips.ProcessPipExecutionPerformance;
using ReportedFileAccess = BuildXL.Processes.ReportedFileAccess;
using ReportedProcess = BuildXL.Processes.ReportedProcess;
using SealDirectory = BuildXL.Pips.Operations.SealDirectory;
using SealDirectoryKind = BuildXL.Xldb.Proto.SealDirectoryKind;
using SemanticPathInfo = BuildXL.Pips.SemanticPathInfo;
using ServiceInfo = BuildXL.Xldb.Proto.ServiceInfo;
using ServicePipKind = BuildXL.Xldb.Proto.ServicePipKind;
using UnsafeOptions = BuildXL.Scheduler.Fingerprints.UnsafeOptions;
using WriteFile = BuildXL.Pips.Operations.WriteFile;
using WriteFileEncoding = BuildXL.Xldb.Proto.WriteFileEncoding;

/// Many enums have been shifted or incremented and this is to avoid protobuf's design to not serialize 
/// int/enum values that are equal to 0. Thus we make "0" as an invalid value for each ProtoBuf enum.
namespace BuildXL.Execution.Analyzer
{
    /// <summary>
    /// Extension methods for Xldb ProtoBuf conversions.
    /// </summary>
    public static class XldbProtobufExtensions
    {
        /// <nodoc />
        public static FileArtifactContentDecidedEvent ToFileArtifactContentDecidedEvent(this FileArtifactContentDecidedEventData data, uint workerID, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            return new FileArtifactContentDecidedEvent()
            {
                WorkerID = workerID,
                FileArtifact = data.FileArtifact.ToFileArtifact(pathTable, nameExpander, pathTableMap),
                FileContentInfo = new FileContentInfo
                {
                    LengthAndExistence = new LengthAndExistence()
                    {
                        HasKnownLength = data.FileContentInfo.HasKnownLength,
                        Length = data.FileContentInfo.Length,
                        Existence = data.FileContentInfo.Existence == null ? 0 : (PathExistence)(data.FileContentInfo.Existence + 1)
                    },
                    Hash = data.FileContentInfo.Hash.ToContentHash()
                },
                OutputOrigin = (PipOutputOrigin)(data.OutputOrigin + 1)
            };
        }

        /// <nodoc />
        public static WorkerListEvent ToWorkerListEvent(this WorkerListEventData data, uint workerID)
        {
            var workerListEvent = new WorkerListEvent
            {
                WorkerID = workerID
            };

            workerListEvent.Workers.AddRange(data.Workers);
            return workerListEvent;
        }

        /// <nodoc />
        public static PipExecutionPerformanceEvent ToPipExecutionPerformanceEvent(this PipExecutionPerformanceEventData data)
        {
            var pipExecPerfEvent = new PipExecutionPerformanceEvent();
            var pipExecPerformance = new Xldb.Proto.PipExecutionPerformance();
            pipExecPerformance.PipExecutionLevel = (int)data.ExecutionPerformance.ExecutionLevel;
            pipExecPerformance.ExecutionStart = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(data.ExecutionPerformance.ExecutionStart);
            pipExecPerformance.ExecutionStop = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(data.ExecutionPerformance.ExecutionStop);

            var processPipExecPerformance = new Xldb.Proto.ProcessPipExecutionPerformance();
            var performance = data.ExecutionPerformance as ProcessPipExecutionPerformance;
            if (performance != null)
            {
                processPipExecPerformance.ProcessExecutionTime = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(performance.ProcessExecutionTime);
                processPipExecPerformance.ReadCounters = new IOTypeCounters()
                {
                    OperationCount = performance.IO.ReadCounters.OperationCount,
                    TransferCount = performance.IO.ReadCounters.TransferCount
                };

                processPipExecPerformance.WriteCounters = new IOTypeCounters()
                {
                    OperationCount = performance.IO.WriteCounters.OperationCount,
                    TransferCount = performance.IO.WriteCounters.TransferCount
                };

                processPipExecPerformance.OtherCounters = new IOTypeCounters()
                {
                    OperationCount = performance.IO.OtherCounters.OperationCount,
                    TransferCount = performance.IO.OtherCounters.TransferCount
                };

                processPipExecPerformance.UserTime = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(performance.UserTime);
                processPipExecPerformance.KernelTime = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(performance.KernelTime);
                processPipExecPerformance.PeakMemoryUsageMb = performance.MemoryCounters.PeakWorkingSetMb;
                processPipExecPerformance.NumberOfProcesses = performance.NumberOfProcesses;

                processPipExecPerformance.FileMonitoringViolationCounters = new Xldb.Proto.FileMonitoringViolationCounters()
                {
                    NumFileAccessesWhitelistedAndCacheable = performance.FileMonitoringViolations.NumFileAccessesWhitelistedAndCacheable,
                    NumFileAccessesWhitelistedButNotCacheable = performance.FileMonitoringViolations.NumFileAccessesWhitelistedButNotCacheable,
                    NumFileAccessViolationsNotWhitelisted = performance.FileMonitoringViolations.NumFileAccessViolationsNotWhitelisted
                };

                processPipExecPerformance.Fingerprint = performance.Fingerprint.ToFingerprint();

                if (performance.CacheDescriptorId.HasValue)
                {
                    processPipExecPerformance.CacheDescriptorId = performance.CacheDescriptorId.Value;
                }
            }

            pipExecPerfEvent.WorkerID = data.ExecutionPerformance.WorkerId;
            pipExecPerfEvent.PipID = data.PipId.Value;
            pipExecPerfEvent.PipExecutionPerformance = pipExecPerformance;
            pipExecPerfEvent.ProcessPipExecutionPerformance = processPipExecPerformance;
            return pipExecPerfEvent;
        }

        /// <nodoc />
        public static DirectoryMembershipHashedEvent ToDirectoryMembershipHashedEvent(this DirectoryMembershipHashedEventData data, uint workerID, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            pathTableMap.TryAdd(data.Directory.ToString(pathTable, PathFormat.Windows, nameExpander), data.Directory.RawValue);
            
            var directoryMembershipEvent = new DirectoryMembershipHashedEvent()
            {
                WorkerID = workerID,
                DirectoryFingerprint = new DirectoryFingerprint()
                {
                    Hash = data.DirectoryFingerprint.Hash.ToContentHash()
                },
                DirectoryPath = data.Directory.RawValue,
                IsStatic = data.IsSearchPath,
                IsSearchPath = data.IsSearchPath,
                PipID = data.PipId.Value,
                EnumeratePatternRegex = data.EnumeratePatternRegex ?? ""
            };
            
            foreach(var member in data.Members)
            {
                directoryMembershipEvent.MemberPaths.Add(member.RawValue);
                pathTableMap.TryAdd(member.ToString(pathTable, PathFormat.Windows, nameExpander), member.RawValue);
            }
            
            return directoryMembershipEvent;
        }

        /// <nodoc />
        public static ProcessExecutionMonitoringReportedEvent ToProcessExecutionMonitoringReportedEvent(this ProcessExecutionMonitoringReportedEventData data, uint workerID, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            var processExecutionMonitoringReportedEvent = new ProcessExecutionMonitoringReportedEvent
            {
                WorkerID = workerID,
                PipID = data.PipId.Value
            };

            processExecutionMonitoringReportedEvent.ReportedProcesses.AddRange(
                data.ReportedProcesses.Select(rp => rp.ToReportedProcess()));
            processExecutionMonitoringReportedEvent.ReportedFileAccesses.AddRange(
                data.ReportedFileAccesses.Select(reportedFileAccess => reportedFileAccess.ToReportedFileAccess(pathTable, nameExpander, pathTableMap)));
            processExecutionMonitoringReportedEvent.WhitelistedReportedFileAccesses.AddRange(
                data.WhitelistedReportedFileAccesses.Select(
                    whiteListReportedFileAccess => whiteListReportedFileAccess.ToReportedFileAccess(pathTable, nameExpander, pathTableMap)));

            foreach (var processDetouringStatus in data.ProcessDetouringStatuses)
            {
                processExecutionMonitoringReportedEvent.ProcessDetouringStatuses.Add(new Xldb.Proto.ProcessDetouringStatusData()
                {
                    ProcessID = processDetouringStatus.ProcessId,
                    ReportStatus = processDetouringStatus.ReportStatus,
                    ProcessName = processDetouringStatus.ProcessName,
                    StartApplicationName = processDetouringStatus.StartApplicationName,
                    StartCommandLine = processDetouringStatus.StartCommandLine,
                    NeedsInjection = processDetouringStatus.NeedsInjection,
                    Job = processDetouringStatus.Job,
                    DisableDetours = processDetouringStatus.DisableDetours,
                    CreationFlags = processDetouringStatus.CreationFlags,
                    Detoured = processDetouringStatus.Detoured,
                    Error = processDetouringStatus.Error,
                    CreateProcessStatusReturn = processDetouringStatus.CreateProcessStatusReturn
                });
            }

            return processExecutionMonitoringReportedEvent;
        }

        /// <nodoc />
        public static ProcessFingerprintComputationEvent ToProcessFingerprintComputationEvent(this ProcessFingerprintComputationEventData data, uint workerID, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            var processFingerprintComputationEvent = new ProcessFingerprintComputationEvent
            {
                WorkerID = workerID,
                Kind = (Xldb.Proto.FingerprintComputationKind)(data.Kind + 1),
                PipID = data.PipId.Value,
                WeakFingerprint = new WeakContentFingerprint()
                {
                    Hash = data.WeakFingerprint.Hash.ToFingerprint()
                },
            };

            foreach (var strongFingerprintComputation in data.StrongFingerprintComputations)
            {
                var processStrongFingerprintComputationData = new Xldb.Proto.ProcessStrongFingerprintComputationData()
                {
                    PathSet = strongFingerprintComputation.PathSet.ToObservedPathSet(pathTable, nameExpander, pathTableMap, stringTableMap),
                    PathSetHash = strongFingerprintComputation.PathSetHash.ToContentHash(),
                    UnsafeOptions = strongFingerprintComputation.UnsafeOptions.ToUnsafeOptions(),
                    Succeeded = strongFingerprintComputation.Succeeded,
                    IsStrongFingerprintHit = strongFingerprintComputation.IsStrongFingerprintHit,
                    ComputedStrongFingerprint = new StrongContentFingerprint()
                    {
                        Hash = strongFingerprintComputation.ComputedStrongFingerprint.Hash.ToFingerprint()
                    }
                };

                processStrongFingerprintComputationData.PathEntries.AddRange(
                    strongFingerprintComputation.PathEntries.Select(
                        pathEntry => pathEntry.ToObservedPathEntry(pathTable, nameExpander, pathTableMap)));
                processStrongFingerprintComputationData.ObservedAccessedFileNames.AddRange(
                    strongFingerprintComputation.ObservedAccessedFileNames.Select(
                        observedAccessedFileName => observedAccessedFileName.ToString(pathTable, stringTableMap)));
                processStrongFingerprintComputationData.PriorStrongFingerprints.AddRange(
                    strongFingerprintComputation.PriorStrongFingerprints.Select(
                        priorStrongFingerprint => new StrongContentFingerprint() { Hash = priorStrongFingerprint.Hash.ToFingerprint() }));

                foreach (var observedInput in strongFingerprintComputation.ObservedInputs)
                {
                    pathTableMap.TryAdd(observedInput.Path.ToString(pathTable, PathFormat.Windows, nameExpander), observedInput.Path.RawValue);

                    processStrongFingerprintComputationData.ObservedInputs.Add(new ObservedInput()
                    {
                        Type = (ObservedInputType)(observedInput.Type + 1),
                        Hash = observedInput.Hash.ToContentHash(),
                        PathEntry = observedInput.PathEntry.ToObservedPathEntry(pathTable, nameExpander, pathTableMap),
                        Path = observedInput.Path.RawValue,
                        IsSearchPath = observedInput.IsSearchPath,
                        IsDirectoryPath = observedInput.IsDirectoryPath,
                        DirectoryEnumeration = observedInput.DirectoryEnumeration
                    });
                }

                processFingerprintComputationEvent.StrongFingerprintComputations.Add(processStrongFingerprintComputationData);
            }

            return processFingerprintComputationEvent;
        }

        /// <nodoc />
        public static BuildSessionConfigurationEvent ToExecutionLogSaltsData(this BuildSessionConfigurationEventData data, uint workerID)
        {
            return new BuildSessionConfigurationEvent
            {
                WorkerID = workerID,
                DisableDetours = data.DisableDetours,
                IgnoreReparsePoints = data.IgnoreReparsePoints,
                IgnorePreloadedDlls = data.IgnorePreloadedDlls,
                ExistingDirectoryProbesAsEnumerations = data.ExistingDirectoryProbesAsEnumerations,
                NtFileCreateMonitored = data.NtFileCreateMonitored,
                ZwFileCreateOpenMonitored = data.ZwFileCreateOpenMonitored,
                IgnoreZwRenameFileInformation = data.IgnoreZwRenameFileInformation,
                IgnoreZwOtherFileInformation = data.IgnoreZwOtherFileInformation,
                IgnoreNonCreateFileReparsePoints = data.IgnoreNonCreateFileReparsePoints,
                IgnoreSetFileInformationByHandle = data.IgnoreSetFileInformationByHandle,
                IgnoreGetFinalPathNameByHandle = data.IgnoreGetFinalPathNameByHandle,
                FingerprintVersion = (int)data.FingerprintVersion,
                FingerprintSalt = data.FingerprintSalt,
                SearchPathToolsHash = data.SearchPathToolsHash != null ? ((ContentHash)data.SearchPathToolsHash).ToContentHash() : null,
                UnexpectedFileAccessesAreErrors = data.UnexpectedFileAccessesAreErrors,
                MonitorFileAccesses = data.MonitorFileAccesses,
                MaskUntrackedAccesses = data.MaskUntrackedAccesses,
                NormalizeReadTimestamps = data.NormalizeReadTimestamps,
                PipWarningsPromotedToErrors = data.PipWarningsPromotedToErrors,
                ValidateDistribution = data.ValidateDistribution,
                RequiredKextVersionNumber = data.RequiredKextVersionNumber
            };
        }

        /// <nodoc />
        public static DependencyViolationReportedEvent ToDependencyViolationReportedEvent(this DependencyViolationEventData data, uint workerID, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            pathTableMap.TryAdd(data.Path.ToString(pathTable, PathFormat.Windows, nameExpander), data.Path.RawValue);

            return new DependencyViolationReportedEvent()
            {
                WorkerID = workerID,
                ViolatorPipID = data.ViolatorPipId.Value,
                RelatedPipID = data.RelatedPipId.Value,
                ViolationType = (FileMonitoringViolationAnalyzer_DependencyViolationType)(data.ViolationType + 1),
                AccessLevel = (FileMonitoringViolationAnalyzer_AccessLevel)(data.AccessLevel + 1),
                Path = data.Path.RawValue
            };
        }

        /// <nodoc />
        public static PipExecutionStepPerformanceReportedEvent ToPipExecutionStepPerformanceReportedEvent(this PipExecutionStepPerformanceEventData data, uint workerID)
        {
            var pipExecStepPerformanceEvent = new PipExecutionStepPerformanceReportedEvent
            {
                WorkerID = workerID,
                PipID = data.PipId.Value,
                StartTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(data.StartTime),
                Duration = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(data.Duration),
                Step = (PipExecutionStep)(data.Step + 1),
                Dispatcher = (WorkDispatcher_DispatcherKind)(data.Dispatcher + 1)
            };

            return pipExecStepPerformanceEvent;
        }

        /// <nodoc />
        public static PipCacheMissEvent ToPipCacheMissEvent(this PipCacheMissEventData data, uint workerID)
        {
            return new PipCacheMissEvent()
            {
                WorkerID = workerID,
                PipID = data.PipId.Value,
                CacheMissType = (PipCacheMissType)data.CacheMissType
            };
        }

        /// <nodoc />
        public static StatusReportedEvent ToResourceUsageReportedEvent(this StatusEventData data, uint workerID)
        {
            var statusReportedEvent = new StatusReportedEvent()
            {
                WorkerID = workerID,
                Time = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(data.Time),
                CpuPercent = data.CpuPercent,
                RamPercent = data.RamPercent,
                MachineRamUtilizationMB = data.RamUsedMb,
                CommitPercent = data.CommitPercent,
                CommitTotalMB = data.CommitUsedMb,
                ProcessCpuPercent = data.ProcessCpuPercent,
                ProcessWorkingSetMB = data.ProcessWorkingSetMB,
                CpuWaiting = data.CpuWaiting,
                CpuRunning = data.CpuRunning,
                IoCurrentMax = data.IoCurrentMax,
                IoWaiting = data.IoWaiting,
                IoRunning = data.IoRunning,
                LookupWaiting = data.LookupWaiting,
                LookupRunning = data.LookupRunning,
                RunningPipExecutorProcesses = data.RunningPipExecutorProcesses,
                RunningProcesses = data.RunningProcesses,
                LimitingResource = (ExecutionSampler_LimitingResource)(data.LimitingResource + 1),
                UnresponsivenessFactor = data.UnresponsivenessFactor,
                ProcessPipsPending = data.ProcessPipsPending,
                ProcessPipsAllocatedSlots = data.ProcessPipsAllocatedSlots
            };

            statusReportedEvent.DiskPercents.AddRange(data.DiskPercents);
            statusReportedEvent.DiskQueueDepths.AddRange(data.DiskQueueDepths);
            statusReportedEvent.PipsSucceededAllTypes.AddRange(data.PipsSucceededAllTypes);

            return statusReportedEvent;
        }

        /// <nodoc />
        public static BxlInvocationEvent ToBxlInvocationEvent(this BxlInvocationEventData data, uint workerID, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            var loggingConfig = data.Configuration.Logging;

            pathTableMap.TryAdd(loggingConfig.SubstSource.ToString(pathTable, PathFormat.Windows, nameExpander), loggingConfig.SubstSource.RawValue);
            pathTableMap.TryAdd(loggingConfig.SubstTarget.ToString(pathTable, PathFormat.Windows, nameExpander), loggingConfig.SubstTarget.RawValue);

            var bxlInvEvent = new BxlInvocationEvent
            {
                WorkerID = workerID,
                SubstSourcePath = loggingConfig.SubstSource.RawValue,
                SubstTargetPath = loggingConfig.SubstTarget.RawValue,
                IsSubstSourceValid = loggingConfig.SubstSource.IsValid,
                IsSubstTargetValid = loggingConfig.SubstTarget.IsValid
            };

            bxlInvEvent.CommandLineArguments.AddRange(data.Configuration.CommandLineArguments);

            return bxlInvEvent;
        }

        public static Xldb.Proto.ContentHash ToContentHash(this ContentHash contentHash)
        {
            return new Xldb.Proto.ContentHash()
            {
                Value = Google.Protobuf.ByteString.CopyFrom(contentHash.ToHashByteArray()),
                HashType = contentHash.HashType == Cache.ContentStore.Hashing.HashType.DeprecatedVso0 ? Xldb.Proto.HashType.DeprecatedVso0 : (Xldb.Proto.HashType)(contentHash.HashType + 1),
            };
        }

        /// <nodoc />
        public static Xldb.Proto.ReportedFileAccess ToReportedFileAccess(this ReportedFileAccess reportedFileAccess, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            pathTableMap.TryAdd(reportedFileAccess.ManifestPath.ToString(pathTable, PathFormat.Windows, nameExpander), reportedFileAccess.ManifestPath.RawValue);

            return new Xldb.Proto.ReportedFileAccess()
            {
                // No need to + 1 here since the Bxl version of the enum never conained a 0 value, so adding Unspecified=0 did not change the bxl->protobuf enum mapping
                CreationDisposition = (Xldb.Proto.CreationDisposition)reportedFileAccess.CreationDisposition,
                // No need to + 1 here since the Bxl version of the enum never conained a 0 value, so adding Unspecified=0 did not change the bxl->protobuf enum mapping
                // However, GENERIC_READ is of value 2^31 in bxl code, but -2^31 in protobuf enum due to 2^31 - 1 being the maximum value of an enum in protobuf. Thus special ternary assignment here.
                DesiredAccess = reportedFileAccess.DesiredAccess == Processes.DesiredAccess.GENERIC_READ ? Xldb.Proto.DesiredAccess.GenericRead : (Xldb.Proto.DesiredAccess)reportedFileAccess.DesiredAccess,
                Error = reportedFileAccess.Error,
                Usn = reportedFileAccess.Usn.Value,
                // No need to + 1 here since the Bxl version of the enum never conained a 0 value, so adding Unspecified=0 did not change the bxl->protobuf enum mapping
                // However, WRITE_THROUGH is of value 2^31 in bxl code, but -2^31 in protobuf enum due to 2^31 - 1 being the maximum value of an enum in protobuf. Thus special ternary assignment here.
                FlagsAndAttributes = reportedFileAccess.FlagsAndAttributes == Processes.FlagsAndAttributes.FILE_FLAG_WRITE_THROUGH ? Xldb.Proto.FlagsAndAttributes.FileFlagWriteThrough : (Xldb.Proto.FlagsAndAttributes)reportedFileAccess.FlagsAndAttributes,
                Path = reportedFileAccess.Path,
                ManifestPath = reportedFileAccess.ManifestPath.RawValue,
                Process = reportedFileAccess.Process.ToReportedProcess(),
                ShareMode = reportedFileAccess.ShareMode == Processes.ShareMode.FILE_SHARE_NONE ? Xldb.Proto.ShareMode.FileShareNone : (Xldb.Proto.ShareMode)((int)reportedFileAccess.ShareMode << 1),
                Status = (Xldb.Proto.FileAccessStatus)(reportedFileAccess.Status + 1),
                Method = (Xldb.Proto.FileAccessStatusMethod)(reportedFileAccess.Method + 1),
                RequestedAccess = reportedFileAccess.RequestedAccess == Processes.RequestedAccess.None ? Xldb.Proto.RequestedAccess.None : (Xldb.Proto.RequestedAccess)((int)reportedFileAccess.RequestedAccess << 1),
                Operation = (Xldb.Proto.ReportedFileOperation)(reportedFileAccess.Operation + 1),
                ExplicitlyReported = reportedFileAccess.ExplicitlyReported,
                EnumeratePattern = reportedFileAccess.EnumeratePattern
            };
        }

        /// <nodoc />
        public static Xldb.Proto.ObservedPathSet ToObservedPathSet(this ObservedPathSet pathSet, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            var observedPathSet = new Xldb.Proto.ObservedPathSet();
            observedPathSet.Paths.AddRange(pathSet.Paths.Select(pathEntry => pathEntry.ToObservedPathEntry(pathTable, nameExpander, pathTableMap)));
            observedPathSet.ObservedAccessedFileNames.AddRange(
                pathSet.ObservedAccessedFileNames.Select(
                    observedAccessedFileName => observedAccessedFileName.ToString(pathTable, stringTableMap)));
            observedPathSet.UnsafeOptions = pathSet.UnsafeOptions.ToUnsafeOptions();

            return observedPathSet;
        }

        /// <nodoc />
        public static Xldb.Proto.UnsafeOptions ToUnsafeOptions(this UnsafeOptions unsafeOption)
        {
            var unsafeOpt = new Xldb.Proto.UnsafeOptions()
            {
                PreserveOutputsInfo = new PreserveOutputsInfo()
                {
                    Salt = unsafeOption.PreserveOutputsSalt.Salt.ToContentHash(),
                    PreserveOutputTrustLevel = unsafeOption.PreserveOutputsSalt.PreserveOutputTrustLevel
                },

                UnsafeConfiguration = new UnsafeSandboxConfiguration()
                {
                    PreserveOutputs = (PreserveOutputsMode)(unsafeOption.UnsafeConfiguration.PreserveOutputs + 1),
                    MonitorFileAccesses = unsafeOption.UnsafeConfiguration.MonitorFileAccesses,
                    IgnoreZwRenameFileInformation = unsafeOption.UnsafeConfiguration.IgnoreZwRenameFileInformation,
                    IgnoreZwOtherFileInformation = unsafeOption.UnsafeConfiguration.IgnoreZwOtherFileInformation,
                    IgnoreNonCreateFileReparsePoints = unsafeOption.UnsafeConfiguration.IgnoreNonCreateFileReparsePoints,
                    IgnoreSetFileInformationByHandle = unsafeOption.UnsafeConfiguration.IgnoreSetFileInformationByHandle,
                    IgnoreReparsePoints = unsafeOption.UnsafeConfiguration.IgnoreReparsePoints,
                    IgnorePreloadedDlls = unsafeOption.UnsafeConfiguration.IgnorePreloadedDlls,
                    ExistingDirectoryProbesAsEnumerations = unsafeOption.UnsafeConfiguration.ExistingDirectoryProbesAsEnumerations,
                    MonitorNtCreateFile = unsafeOption.UnsafeConfiguration.MonitorNtCreateFile,
                    MonitorZwCreateOpenQueryFile = unsafeOption.UnsafeConfiguration.MonitorZwCreateOpenQueryFile,
                    SandboxKind = (SandboxKind)(unsafeOption.UnsafeConfiguration.SandboxKind + 1),
                    UnexpectedFileAccessesAreErrors = unsafeOption.UnsafeConfiguration.UnexpectedFileAccessesAreErrors,
                    IgnoreGetFinalPathNameByHandle = unsafeOption.UnsafeConfiguration.IgnoreGetFinalPathNameByHandle,
                    IgnoreDynamicWritesOnAbsentProbes = (DynamicWriteOnAbsentProbePolicy)(unsafeOption.UnsafeConfiguration.IgnoreDynamicWritesOnAbsentProbes),
                    IgnoreUndeclaredAccessesUnderSharedOpaques = unsafeOption.UnsafeConfiguration.IgnoreUndeclaredAccessesUnderSharedOpaques,
                }
            };

            if (unsafeOption.UnsafeConfiguration.DoubleWritePolicy != null)
            {
                unsafeOpt.UnsafeConfiguration.DoubleWritePolicy = (DoubleWritePolicy)(unsafeOption.UnsafeConfiguration.DoubleWritePolicy + 1);
            }

            return unsafeOpt;
        }

        public static int ToString(this StringId stringId, PathTable pathTable, ConcurrentBigMap<string, int> stringTableMap)
        {
            // A return value of 0 will not be serialized in ProtoBuf
            if (!stringId.IsValid)
            {
                return 0;
            }

            stringTableMap.TryAdd(stringId.ToString(pathTable.StringTable), stringId.Value);
            return stringId.Value;
        }

        /// <nodoc />
        public static Xldb.Proto.FileArtifact ToFileArtifact(this FileArtifact fileArtifact, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            if (!fileArtifact.IsValid)
            {
                return null;
            }

            pathTableMap.TryAdd(fileArtifact.Path.ToString(pathTable, PathFormat.Windows, nameExpander), fileArtifact.Path.RawValue);

            return new Xldb.Proto.FileArtifact
            {
                Path = fileArtifact.Path.RawValue,
                RewriteCount = fileArtifact.RewriteCount,
            };
        }

        /// <nodoc />
        public static Xldb.Proto.ReportedProcess ToReportedProcess(this ReportedProcess reportedProcess)
        {
            return new Xldb.Proto.ReportedProcess()
            {
                Path = reportedProcess.Path,
                ProcessId = reportedProcess.ProcessId,
                ProcessArgs = reportedProcess.ProcessArgs,
                ReadCounters = new IOTypeCounters
                {
                    OperationCount = reportedProcess.IOCounters.ReadCounters.OperationCount,
                    TransferCount = reportedProcess.IOCounters.ReadCounters.TransferCount
                },
                WriteCounters = new IOTypeCounters
                {
                    OperationCount = reportedProcess.IOCounters.WriteCounters.OperationCount,
                    TransferCount = reportedProcess.IOCounters.WriteCounters.TransferCount
                },
                OtherCounters = new IOTypeCounters
                {
                    OperationCount = reportedProcess.IOCounters.OtherCounters.OperationCount,
                    TransferCount = reportedProcess.IOCounters.OtherCounters.TransferCount
                },
                CreationTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(reportedProcess.CreationTime),
                ExitTime = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(reportedProcess.ExitTime),
                KernelTime = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(reportedProcess.KernelTime),
                UserTime = Google.Protobuf.WellKnownTypes.Duration.FromTimeSpan(reportedProcess.UserTime),
                ExitCode = reportedProcess.ExitCode,
                ParentProcessId = reportedProcess.ParentProcessId
            };
        }

        /// <nodoc />
        public static Xldb.Proto.ObservedPathEntry ToObservedPathEntry(this ObservedPathEntry pathEntry, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            pathTableMap.Add(pathEntry.Path.ToString(pathTable, PathFormat.Windows, nameExpander), pathEntry.Path.RawValue);

            return new Xldb.Proto.ObservedPathEntry()
            {
                Path = pathEntry.Path.RawValue,
                EnumeratePatternRegex = pathEntry.EnumeratePatternRegex ?? "",
                IsSearchPath = pathEntry.IsSearchPath,
                IsDirectoryPath = pathEntry.IsDirectoryPath,
                DirectoryEnumeration = pathEntry.DirectoryEnumeration,
                DirectoryEnumerationWithCustomPattern = pathEntry.DirectoryEnumerationWithCustomPattern,
                DirectoryEnumerationWithAllPattern = pathEntry.DirectoryEnumerationWithAllPattern,
                IsFileProbe = pathEntry.IsFileProbe
            };
        }

        /// <nodoc />
        public static Xldb.Proto.Fingerprint ToFingerprint(this Fingerprint fingerprint)
        {
            return new Xldb.Proto.Fingerprint()
            {
                Length = fingerprint.Length,
                Bytes = Google.Protobuf.ByteString.CopyFrom(fingerprint.ToByteArray())
            };
        }

        /// <nodoc />
        public static Xldb.Proto.DirectoryArtifact ToDirectoryArtifact(this DirectoryArtifact artifact, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            if (!artifact.IsValid)
            {
                return null;
            }

            pathTableMap.TryAdd(artifact.Path.ToString(pathTable, PathFormat.Windows, nameExpander), artifact.Path.RawValue);

            return new Xldb.Proto.DirectoryArtifact()
            {
                Path = artifact.Path.RawValue,
                PartialSealID = artifact.PartialSealId,
                IsSharedOpaque = artifact.IsSharedOpaque
            };
        }

        /// <nodoc />
        public static Xldb.Proto.PipProvenance ToPipProvenance(this PipProvenance provenance, PathTable pathTable, ConcurrentBigMap<string, int> stringTableMap)
        {
            return provenance == null ? null : new Xldb.Proto.PipProvenance()
            {
                Usage = provenance.Usage.IsValid ? provenance.Usage.ToString(pathTable) : "",
                ModuleId = provenance.ModuleId.Value.ToString(pathTable, stringTableMap),
                ModuleName = provenance.ModuleName.ToString(pathTable, stringTableMap),
                SemiStableHash = provenance.SemiStableHash
            };
        }

        /// <nodoc />
        public static Xldb.Proto.FileOrDirectoryArtifact ToFileOrDirectoryArtifact(this FileOrDirectoryArtifact artifact, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap)
        {
            if (!artifact.IsValid)
            {
                return null;
            }

            var xldbFileOrDirectoryArtifact = new Xldb.Proto.FileOrDirectoryArtifact();
            if (artifact.IsDirectory)
            {
                xldbFileOrDirectoryArtifact.IsDirectory = true;
                xldbFileOrDirectoryArtifact.DirectoryArtifact = artifact.DirectoryArtifact.ToDirectoryArtifact(pathTable, nameExpander, pathTableMap);
            }
            else
            {
                xldbFileOrDirectoryArtifact.IsFile = true;
                xldbFileOrDirectoryArtifact.FileArtifact = artifact.FileArtifact.ToFileArtifact(pathTable, nameExpander, pathTableMap);
            }

            return xldbFileOrDirectoryArtifact;
        }

        /// <nodoc />
        public static Xldb.Proto.Pip ToPip(this Pip pip, CachedGraph cachedGraph)
        {
            var xldbPip = new Xldb.Proto.Pip()
            {
                SemiStableHash = pip.SemiStableHash,
                PipId = pip.PipId.Value,
            };

            foreach (var incomingEdge in cachedGraph.DirectedGraph.GetIncomingEdges(pip.PipId.ToNodeId()))
            {
                var pipType = cachedGraph.PipTable.HydratePip(incomingEdge.OtherNode.ToPipId(), Pips.PipQueryContext.Explorer).PipType;

                if (pipType != PipType.Value && pipType != PipType.HashSourceFile && pipType != PipType.SpecFile && pipType != PipType.Module)
                {
                    xldbPip.IncomingEdges.Add(incomingEdge.OtherNode.Value);
                }
            }

            foreach (var outgoingEdge in cachedGraph.DirectedGraph.GetOutgoingEdges(pip.PipId.ToNodeId()))
            {
                var pipType = cachedGraph.PipTable.HydratePip(outgoingEdge.OtherNode.ToPipId(), Pips.PipQueryContext.Explorer).PipType;

                if (pipType != PipType.Value && pipType != PipType.HashSourceFile && pipType != PipType.SpecFile && pipType != PipType.Module)
                {
                    xldbPip.OutgoingEdges.Add(outgoingEdge.OtherNode.Value);
                }
            }

            return xldbPip;
        }

        /// <nodoc />
        public static Xldb.Proto.SealDirectory ToSealDirectory(this SealDirectory pip, PathTable pathTable, Xldb.Proto.Pip parentPip, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            var xldbSealDirectory = new Xldb.Proto.SealDirectory
            {
                GraphInfo = parentPip,
                Kind = (SealDirectoryKind)(pip.Kind + 1),
                IsComposite = pip.IsComposite,
                Scrub = pip.Scrub,
                Directory = pip.Directory.ToDirectoryArtifact(pathTable, nameExpander, pathTableMap),
                IsSealSourceDirectory = pip.IsSealSourceDirectory,
                Provenance = pip.Provenance.ToPipProvenance(pathTable, stringTableMap),
            };

            xldbSealDirectory.Patterns.AddRange(pip.Patterns.Select(key => key.ToString(pathTable, stringTableMap)));
            xldbSealDirectory.Contents.AddRange(pip.Contents.Select(file => file.ToFileArtifact(pathTable, nameExpander, pathTableMap)));
            xldbSealDirectory.ComposedDirectories.AddRange(pip.ComposedDirectories.Select(dir => dir.ToDirectoryArtifact(pathTable, nameExpander, pathTableMap)));

            if (pip.Tags.IsValid)
            {
                xldbSealDirectory.Tags.AddRange(pip.Tags.Select(key => key.ToString(pathTable, stringTableMap)));
            }

            return xldbSealDirectory;
        }

        /// <nodoc />
        public static Xldb.Proto.CopyFile ToCopyFile(this CopyFile pip, PathTable pathTable, Xldb.Proto.Pip parentPip, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            var xldbCopyFile = new Xldb.Proto.CopyFile
            {
                GraphInfo = parentPip,
                Source = pip.Source.ToFileArtifact(pathTable, nameExpander, pathTableMap),
                Destination = pip.Destination.ToFileArtifact(pathTable, nameExpander, pathTableMap),
                OutputsMustRemainWritable = pip.OutputsMustRemainWritable,
                Provenance = pip.Provenance.ToPipProvenance(pathTable, stringTableMap),
            };

            if (pip.Tags.IsValid)
            {
                xldbCopyFile.Tags.AddRange(pip.Tags.Select(key => key.ToString(pathTable, stringTableMap)));
            }

            return xldbCopyFile;
        }

        /// <nodoc />
        public static Xldb.Proto.WriteFile ToWriteFile(this WriteFile pip, PathTable pathTable, Xldb.Proto.Pip parentPip, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            var xldbWriteFile = new Xldb.Proto.WriteFile
            {
                GraphInfo = parentPip,
                Destination = pip.Destination.ToFileArtifact(pathTable, nameExpander, pathTableMap),
                Contents = pip.Contents.IsValid ? pip.Contents.ToString(pathTable) : "",
                Encoding = (WriteFileEncoding)(pip.Encoding + 1),
                Provenance = pip.Provenance.ToPipProvenance(pathTable, stringTableMap),
            };

            if (pip.Tags.IsValid)
            {
                xldbWriteFile.Tags.AddRange(pip.Tags.Select(key => key.ToString(pathTable, stringTableMap)));
            }

            return xldbWriteFile;
        }

        /// <nodoc />
        public static ProcessPip ToProcessPip(this Process pip, PathTable pathTable, Xldb.Proto.Pip parentPip, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            pathTableMap.TryAdd(pip.WorkingDirectory.ToString(pathTable, PathFormat.Windows, nameExpander), pip.WorkingDirectory.RawValue);
            pathTableMap.TryAdd(pip.TempDirectory.ToString(pathTable, PathFormat.Windows, nameExpander), pip.TempDirectory.RawValue);

            var xldbProcessPip = new ProcessPip
            {
                GraphInfo = parentPip,
                ProcessOptions = pip.ProcessOptions == Process.Options.None ? Options.None : (Options) ((int)pip.ProcessOptions << 1),
                StandardInputFile = pip.StandardInputFile.ToFileArtifact(pathTable, nameExpander, pathTableMap),
                StandardInputData = pip.StandardInputData.IsValid ? pip.StandardInputData.ToString(pathTable) : "",
                StandardInput = !pip.StandardInput.IsValid ? null : new Xldb.Proto.StandardInput()
                {
                    File = pip.StandardInput.File.ToFileArtifact(pathTable, nameExpander, pathTableMap),
                    Data = pip.StandardInput.Data.ToString(pathTable),
                    IsFile = pip.StandardInput.IsFile,
                    IsData = pip.StandardInput.IsData
                },
                ResponseFile = pip.ResponseFile.ToFileArtifact(pathTable, nameExpander, pathTableMap),
                ResponseFileData = pip.ResponseFileData.IsValid ? pip.ResponseFileData.ToString(pathTable) : "",
                Executable = pip.Executable.ToFileArtifact(pathTable, nameExpander, pathTableMap),
                ToolDescription = pip.ToolDescription.ToString(pathTable, stringTableMap),
                WorkingDirectoryPath = pip.WorkingDirectory.RawValue,
                Arguments = pip.Arguments.IsValid ? pip.Arguments.ToString(pathTable) : "",
                TempDirectoryPath = pip.TempDirectory.RawValue,
                Provenance = pip.Provenance.ToPipProvenance(pathTable, stringTableMap),
            };

            if (pip.ServiceInfo.IsValid)
            {
                var serviceInfo = new ServiceInfo
                {
                    Kind = (ServicePipKind)(pip.ServiceInfo.Kind + 1),
                    ShutdownPipId = pip.ServiceInfo.ShutdownPipId.Value,
                    IsStartOrShutdownKind = pip.ServiceInfo.IsStartOrShutdownKind
                };

                serviceInfo.ServicePipDependencies.AddRange(pip.ServiceInfo.ServicePipDependencies.Select(key => key.Value));
                serviceInfo.FinalizationPipIds.AddRange(pip.ServiceInfo.FinalizationPipIds.Select(key => key.Value));
                xldbProcessPip.ServiceInfo = serviceInfo;
            }

            xldbProcessPip.EnvironmentVariable.AddRange(pip.EnvironmentVariables.Select(
                envVar => new EnvironmentVariable()
                {
                    Name = envVar.Name.ToString(pathTable, stringTableMap),
                    Value = envVar.Value.IsValid ? envVar.Value.ToString(pathTable) : "",
                    IsPassThrough = envVar.IsPassThrough
                }));
            xldbProcessPip.Dependencies.AddRange(pip.Dependencies.Select(file => file.ToFileArtifact(pathTable, nameExpander, pathTableMap)));
            xldbProcessPip.DirectoryDependencies.AddRange(pip.DirectoryDependencies.Select(dir => dir.ToDirectoryArtifact(pathTable, nameExpander, pathTableMap)));

            foreach(var path in pip.UntrackedPaths)
            {
                pathTableMap.TryAdd(path.ToString(pathTable, PathFormat.Windows, nameExpander), path.RawValue);
                xldbProcessPip.UntrackedPaths.Add(path.RawValue);
            }

            foreach (var path in pip.UntrackedScopes)
            {
                pathTableMap.TryAdd(path.ToString(pathTable, PathFormat.Windows, nameExpander), path.RawValue);
                xldbProcessPip.UntrackedScopesPaths.Add(path.RawValue);
            }

            foreach (var path in pip.AdditionalTempDirectories)
            {
                pathTableMap.TryAdd(path.ToString(pathTable, PathFormat.Windows, nameExpander), path.RawValue);
                xldbProcessPip.AdditionalTempDirectoriesPaths.Add(path.RawValue);
            }

            foreach (var path in pip.PreserveOutputWhitelist)
            {
                pathTableMap.TryAdd(path.ToString(pathTable, PathFormat.Windows, nameExpander), path.RawValue);
                xldbProcessPip.PreserveOutputWhitelistPaths.Add(path.RawValue);
            }

            foreach(var output in pip.FileOutputs)
            {
                if (output.IsValid)
                {
                    pathTableMap.TryAdd(output.Path.ToString(pathTable, PathFormat.Windows, nameExpander), output.Path.RawValue);
                    xldbProcessPip.FileOutputs.Add(new Xldb.Proto.FileArtifactWithAttributes()
                    {
                        Path = output.Path.RawValue,
                        RewriteCount = output.RewriteCount,
                        FileExistence = (Xldb.Proto.FileExistence)(output.FileExistence + 1)
                    });
                }
            }

            xldbProcessPip.DirectoryOutputs.AddRange(pip.DirectoryOutputs.Select(dir => dir.ToDirectoryArtifact(pathTable, nameExpander, pathTableMap)));

            if (pip.Tags.IsValid)
            {
                xldbProcessPip.Tags.AddRange(pip.Tags.Select(key => key.ToString(pathTable, stringTableMap)));
            }

            return xldbProcessPip;
        }

        /// <nodoc />
        public static Xldb.Proto.IpcPip ToIpcPip(this IpcPip pip, PathTable pathTable, Xldb.Proto.Pip parentPip, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            var xldbIpcPip = new Xldb.Proto.IpcPip()
            {
                GraphInfo = parentPip,
                IpcInfo = new IpcClientInfo()
                {
                    IpcMonikerId = pip.IpcInfo.IpcMonikerId.ToString(pathTable, stringTableMap),
                },
                MessageBody = pip.MessageBody.IsValid ? pip.MessageBody.ToString(pathTable) : "",
                IsServiceFinalization = pip.IsServiceFinalization,
                Provenance = pip.Provenance.ToPipProvenance(pathTable, stringTableMap),
            };

            if (pip.Tags.IsValid)
            {
                xldbIpcPip.Tags.AddRange(pip.Tags.Select(key => key.ToString(pathTable, stringTableMap)));
            }

            xldbIpcPip.ServicePipDependencies.AddRange(pip.ServicePipDependencies.Select(pipId => pipId.Value));
            xldbIpcPip.FileDependencies.AddRange(pip.FileDependencies.Select(file => file.ToFileArtifact(pathTable, nameExpander, pathTableMap)));
            xldbIpcPip.DirectoryDependencies.AddRange(pip.DirectoryDependencies.Select(directory => directory.ToDirectoryArtifact(pathTable, nameExpander, pathTableMap)));
            xldbIpcPip.LazilyMaterializedDependencies.AddRange(pip.LazilyMaterializedDependencies.Select(dep => dep.ToFileOrDirectoryArtifact(pathTable, nameExpander, pathTableMap)));

            return xldbIpcPip;
        }

        /// <nodoc />
        public static Xldb.Proto.PipGraph ToPipGraph(this PipGraph pipGraph, PathTable pathTable, PipTable pipTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            var xldbPipGraph = new Xldb.Proto.PipGraph()
            {
                GraphId = pipGraph.GraphId.ToString(),
                SemistableFingerprint = new ContentFingerprint() { Hash = pipGraph.SemistableFingerprint.Hash.ToFingerprint() },
                NodeCount = pipTable.StableKeys.Count,
                MaxAbsolutePathIndex = pipGraph.MaxAbsolutePathIndex,
                FileCount = pipGraph.FileCount,
                ContentCount = pipGraph.ContentCount,
                ArtifactContentCount = pipGraph.ArtifactContentCount,
                ApiServerMoniker = pipGraph.ApiServerMoniker.ToString(pathTable, stringTableMap)
            };

            xldbPipGraph.AllSealDirectoriesAndProducers.AddRange(pipGraph.AllSealDirectoriesAndProducers.Select(kvp => new DirectoryArtifactMap()
            {
                Artifact = kvp.Key.ToDirectoryArtifact(pathTable, nameExpander, pathTableMap),
                PipId = kvp.Value.Value
            }));
            xldbPipGraph.StableKeys.AddRange(pipTable.StableKeys.Select(stableKey => stableKey.Value));

            foreach (var kvp in pipGraph.Modules)
            {
                xldbPipGraph.Modules.Add(kvp.Key.Value.ToString(pathTable, stringTableMap), kvp.Value.Value);
            }

            return xldbPipGraph;
        }

        /// <nodoc />
        public static Xldb.Proto.MountPathExpander ToMountPathExpander(this MountPathExpander mount, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            var xldbMountPathExpander = new Xldb.Proto.MountPathExpander();

            foreach(var path in mount.GetWritableRoots())
            {
                pathTableMap.TryAdd(path.ToString(pathTable, PathFormat.Windows, nameExpander), path.RawValue);
                xldbMountPathExpander.WriteableRootsPaths.Add(path.RawValue);
            }

            foreach(var path in mount.GetPathsWithAllowedCreateDirectory())
            {
                pathTableMap.TryAdd(path.ToString(pathTable, PathFormat.Windows, nameExpander), path.RawValue);
                xldbMountPathExpander.PathsWithAllowedCreateDirectory.Add(path.RawValue);
            }

            foreach (var path in mount.GetScrubbableRoots())
            {
                pathTableMap.TryAdd(path.ToString(pathTable, PathFormat.Windows, nameExpander), path.RawValue);
                xldbMountPathExpander.ScrubbableRootsPaths.Add(path.RawValue);
            }

            foreach (var path in mount.GetAllRoots())
            {
                pathTableMap.TryAdd(path.ToString(pathTable, PathFormat.Windows, nameExpander), path.RawValue);
                xldbMountPathExpander.AllRootsPaths.Add(path.RawValue);
            }

            foreach(var kvp in mount.GetAllMountsByName())
            {
                xldbMountPathExpander.MountsByName.Add(kvp.Key, kvp.Value.ToSemanticPathInfo(pathTable, nameExpander, pathTableMap, stringTableMap));
            }

            return xldbMountPathExpander;
        }

        /// <nodoc />
        public static Xldb.Proto.SemanticPathInfo ToSemanticPathInfo(this SemanticPathInfo pathInfo, PathTable pathTable, NameExpander nameExpander, ConcurrentBigMap<string, int> pathTableMap, ConcurrentBigMap<string, int> stringTableMap)
        {
            pathTableMap.TryAdd(pathInfo.Root.ToString(pathTable, PathFormat.Windows, nameExpander), pathInfo.Root.RawValue);
            stringTableMap.TryAdd(pathInfo.RootName.ToString(pathTable.StringTable), pathInfo.RootName.StringId.Value);

            var xldbSemanticPathInfo = new Xldb.Proto.SemanticPathInfo()
            {
                RootPath = pathInfo.Root.RawValue,
                RootName = pathInfo.RootName.StringId.Value,
                IsValid = pathInfo.IsValid,
                AllowHashing = pathInfo.AllowHashing,
                IsReadable = pathInfo.IsReadable,
                IsWriteable = pathInfo.IsWritable,
                AllowCreateDirectory = pathInfo.AllowCreateDirectory,
                IsSystem = pathInfo.IsSystem,
                IsScrubbable = pathInfo.IsScrubbable,
                HasPotentialBuildOutputs = pathInfo.HasPotentialBuildOutputs,
            };

            if (pathInfo.Flags == Pips.SemanticPathFlags.None)
            {
                xldbSemanticPathInfo.Flags = Xldb.Proto.SemanticPathFlags.None;
            }
            else if (pathInfo.Flags == Pips.SemanticPathFlags.Writable)
            {
                xldbSemanticPathInfo.Flags = Xldb.Proto.SemanticPathFlags.Writable;
            }
            else
            {
                xldbSemanticPathInfo.Flags = (Xldb.Proto.SemanticPathFlags)((int)pathInfo.Flags << 1);
            }

            return xldbSemanticPathInfo;
        }
    }
}
