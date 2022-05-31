// -----------------------------------------------------------------------
// <copyright file="ICache.cs" company="Microsoft Corporation">
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
    /// The ICache Interface.
    /// </summary>
    public interface ICache
    {
        /// <summary>
        /// Adds the cache entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        void AddCacheEntry(string workload, string key, object value);

        /// <summary>
        /// Gets the cache entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <returns>Cache entry object.</returns>
        object GetCacheEntry(string workload, string key);

        /// <summary>
        /// Determines whether [is local memory cache].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is local memory cache]; otherwise, <c>false</c>.
        /// </returns>
        bool IsLocalMemoryCache();

        /// <summary>
        /// Gets the cache keys.
        /// </summary>
        /// <returns>List of Cache keys.</returns>
        List<string> GetCacheKeys();

        /// <summary>
        /// Deletes the cache entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        void DeleteCacheEntry(string workload, string key);
    }
}