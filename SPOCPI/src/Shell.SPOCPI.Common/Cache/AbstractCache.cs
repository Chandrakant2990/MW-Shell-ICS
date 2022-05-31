// -----------------------------------------------------------------------
// <copyright file="AbstractCache.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.Common
{
    using System.Collections.Generic;

    /// <summary>
    /// Abstract Cache.
    /// </summary>
    /// <seealso cref="Common.ICache" />
    public abstract class AbstractCache : ICache
    {
        /// <summary>
        /// Builds Cache key.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <returns>Cache key.</returns>
        public static string BuildCacheKey(string workload, string key)
        {
            return workload + "|" + key;
        }

        /// <summary>
        /// Add cache Entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public abstract void AddCacheEntry(string workload, string key, object value);

        /// <summary>
        /// Delete Cache Entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        public abstract void DeleteCacheEntry(string workload, string key);

        /// <summary>
        /// Get Cache Entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <returns>the object.</returns>
        public abstract object GetCacheEntry(string workload, string key);

        /// <summary>
        /// Flag for local memory cache.
        /// </summary>
        /// <returns>flag for local memory cache.</returns>
        public abstract bool IsLocalMemoryCache();

        /// <summary>
        /// Get Cache Keys.
        /// </summary>
        /// <returns>List of Keys.</returns>
        public abstract List<string> GetCacheKeys();
    }
}
