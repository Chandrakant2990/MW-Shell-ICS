// -----------------------------------------------------------------------
// <copyright file="CacheManager.cs" company="Microsoft Corporation">
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
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The Cache Manager.
    /// </summary>
    public static class CacheManager
    {
        /// <summary>
        /// The cache engines.
        /// </summary>
        private static readonly List<ICache> CacheEngines = new List<ICache>();

        /// <summary>
        /// The caching disabled.
        /// </summary>
        private static bool cachingEnabled;

        /// <summary>
        /// Initializes static members of the <see cref="CacheManager"/> class.
        /// </summary>
        static CacheManager()
        {
            cachingEnabled = ConfigHelper.BooleanReader(Constants.IsRedisCacheEnabled, false);
            CacheEngines.Add(new RedisCache());
        }

        /// <summary>
        /// Adds the cache entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public static void AddCacheEntry(string workload, string key, object value)
        {
            AddCacheEntry(workload, key, value, false);
        }

        /// <summary>
        /// Gets the cache keys.
        /// </summary>
        /// <returns>List of cache keys.</returns>
        public static List<string> GetCacheKeys()
        {
            bool localMemoryCache = true;

            List<string> results = new List<string>();

            foreach (ICache engine in CacheEngines)
            {
                if (localMemoryCache)
                {
                    if (engine.IsLocalMemoryCache())
                    {
                        results = engine.GetCacheKeys();
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Adds the cache entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="localMemoryCache">if set to <c>true</c> [local memory cache].</param>
        public static void AddCacheEntry(string workload, string key, object value, bool localMemoryCache)
        {
            if (!cachingEnabled)
            {
                return;
            }

            foreach (ICache engine in CacheEngines)
            {
                if (localMemoryCache)
                {
                    if (engine.IsLocalMemoryCache())
                    {
                        engine.AddCacheEntry(workload, key, value);
                    }
                }
                else
                {
                    engine.AddCacheEntry(workload, key, value);
                }
            }
        }

        /// <summary>
        /// Gets the cache entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <param name="cacheEntry">The cacheEntry.</param>
        /// <returns>Cache entry object.</returns>
        public static object GetCacheEntry(string workload, string key, out string cacheEntry)
        {
            return GetCacheEntry(workload, key, false, out cacheEntry);
        }

        /// <summary>
        /// Gets the cache entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <param name="localMemoryCache">if set to <c>true</c> [local memory cache].</param>
        /// <param name="cacheEntry">The cacheEntry.</param>
        /// <returns>Cache entry object.</returns>
        public static object GetCacheEntry(string workload, string key, bool localMemoryCache, out string cacheEntry)
        {
            cacheEntry = string.Empty;
            if (!cachingEnabled)
            {
                return null;
            }

            foreach (ICache engine in CacheEngines)
            {
                if (localMemoryCache)
                {
                    if (engine.IsLocalMemoryCache())
                    {
                        var result = engine.GetCacheEntry(workload, key);

                        if (result != null)
                        {
                            cacheEntry = result.ToString();
                        }
                    }
                }
                else
                {
                    var result = engine.GetCacheEntry(workload, key);

                    if (result != null)
                    {
                        cacheEntry = result.ToString();
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Deletes the cache entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <param name="cacheengine">The cache engine.</param>
        /// <returns>Deleted cache entry.</returns>
        public static object DeleteCacheEntry(string workload, string key, out string cacheengine)
        {
            return DeleteCacheEntry(workload, key, false, out cacheengine);
        }

        /// <summary>
        /// Deletes the cache entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <param name="localMemoryCache">if set to <c>true</c> [local memory cache].</param>
        /// <param name="cacheengine">The cache engine.</param>
        /// <returns>Deleted cache entry.</returns>
        public static object DeleteCacheEntry(string workload, string key, bool localMemoryCache, out string cacheengine)
        {
            cacheengine = string.Empty;

            if (!cachingEnabled)
            {
                return null;
            }

            foreach (ICache engine in CacheEngines)
            {
                if (localMemoryCache)
                {
                    if (engine.IsLocalMemoryCache())
                    {
                        engine.DeleteCacheEntry(workload, key);
                    }
                }
                else
                {
                    engine.DeleteCacheEntry(workload, key);
                }
            }

            return null;
        }
    }
}
