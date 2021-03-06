﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading.Tasks;
using BuildXL.Cache.ContentStore.Hashing;
using BuildXL.Cache.ContentStore.Interfaces.Results;
using BuildXL.Cache.ContentStore.Tracing.Internal;

namespace BuildXL.Cache.ContentStore.Distributed.Sessions
{
    /// <summary>
    /// Pin in a configurable way.
    /// </summary>
    public interface IConfigurablePin
    {
        /// <summary>
        /// Pin in a configurable way.
        /// </summary>
        Task<IEnumerable<Task<Indexed<PinResult>>>> PinAsync(OperationContext operationContext, IReadOnlyList<ContentHash> contentHashes, PinOperationConfiguration config);
    }
}
