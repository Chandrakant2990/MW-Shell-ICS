// -----------------------------------------------------------------------
// <copyright file="RedisCache.cs" company="Microsoft Corporation">
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
    using System.Diagnostics.CodeAnalysis;
    using Newtonsoft.Json;
    using StackExchange.Redis;

    /// <summary>
    /// The Redis Cache.
    /// </summary>
    /// <seealso cref="Common.AbstractCache" />
    public class RedisCache : AbstractCache
    {
        /// <summary>
        /// Indicates whether Redis cache is initialized or not.
        /// </summary>
        private readonly bool isInitialised = false;

        /// <summary>
        /// The flag for redis cache enabled.
        /// </summary>
        private readonly bool redisCacheEnabled = false;

        /// <summary>
        /// The connection.
        /// </summary>
        private readonly ConnectionMultiplexer connection = null;

        /// <summary>
        /// The lazy connection.
        /// </summary>
        private readonly Lazy<ConnectionMultiplexer> lazyConnection = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisCache"/> class.
        /// </summary>
        public RedisCache()
        {
            this.redisCacheEnabled = ConfigHelper.BooleanReader(Constants.IsRedisCacheEnabled, false);
            if (this.redisCacheEnabled)
            {
                this.lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
                {
                    return ConnectionMultiplexer.Connect(ConfigHelper.StringReader(Constants.RedisConnectionString, default(string)));
                });

                this.connection = this.lazyConnection.Value;

                this.isInitialised = true;
            }
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1126:PrefixCallsCorrectly", Justification = "Reviewed.")]

        /// <summary>
        /// Adds cache Entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public override void AddCacheEntry(string workload, string key, object value)
        {
            if (this.redisCacheEnabled && this.isInitialised)
            {
                IDatabase cache = this.lazyConnection.Value.GetDatabase();
                if (cache != null)
                {
                    cache.StringSet(BuildCacheKey(workload, key), JsonConvert.SerializeObject(value));
                }
            }
        }

        [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1126:PrefixCallsCorrectly", Justification = "Reviewed.")]

        /// <summary>
        /// Deletes Cache Entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        public override void DeleteCacheEntry(string workload, string key)
        {
            if (this.redisCacheEnabled && this.isInitialised)
            {
                IDatabase cache = this.lazyConnection.Value.GetDatabase();
                cache.KeyDelete(BuildCacheKey(workload, key));
            }
        }

        /// <summary>
        /// Get Cache Entry.
        /// </summary>
        /// <param name="workload">The workload.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        /// the object.
        /// </returns>
        public override object GetCacheEntry(string workload, string key)
        {
            if (this.redisCacheEnabled && this.isInitialised)
            {
                IDatabase cache = this.lazyConnection.Value.GetDatabase();
                RedisValue result = cache.StringGet(BuildCacheKey(workload, key));
                return result.HasValue ? JsonConvert.DeserializeObject(result) : null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Flag for local memory cache.
        /// </summary>
        /// <returns>
        /// flag for local memory cache.
        /// </returns>
        public override bool IsLocalMemoryCache()
        {
            return false;
        }

        /// <summary>
        /// Get Cache Keys.
        /// </summary>
        /// <returns>
        /// List of Keys.
        /// </returns>
        public override List<string> GetCacheKeys()
        {
            List<string> cacheKeys = new List<string>();
            var endpoints = this.connection.GetEndPoints();

            if (endpoints.Length > 0)
            {
                var server = this.connection.GetServer(endpoints[0]);
                List<RedisKey> redisKeys = server.Keys() as List<RedisKey>;
                foreach (RedisKey key in redisKeys)
                {
                    cacheKeys.Add(key.ToString());
                }
            }

            return cacheKeys;
        }
    }
}
