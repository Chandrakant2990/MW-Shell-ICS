// -----------------------------------------------------------------------
// <copyright file="RedisController.cs" company="PlaceholderCompany">
// Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.WebHooksManager.UI.Controllers
{
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Authorization;
    using Newtonsoft.Json;
    using Shell.SPOCPI.Common;
    using Shell.SPOCPI.WebHooksManager.UI.Models;

    /// <summary>
    /// Redis Controller.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    [Authorize(Roles = Constants.AdminRole)]
    public class RedisController : Controller
    {
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        [Authorize]
        public IActionResult Index()
        {
            return this.View();
        }

        /// <summary>
        /// Show RedisCache.
        /// </summary>
        /// <param name="key">key of cache item.</param>
        /// <returns>redisModel.</returns>
        [SuppressMessage("Microsoft.Design", "CA1822:Mark members as static", Justification = "Reviewed.")]
        public RedisModel ShowCache(string key)
        {
            var redisModel = new RedisModel();
            if (!string.IsNullOrWhiteSpace(key))
            {
                try
                {
                    CacheManager.GetCacheEntry(Constants.SPOCPI, key, out string cachedItem);
                    if (!string.IsNullOrWhiteSpace(cachedItem))
                    {
                        redisModel.Data = JsonConvert.DeserializeObject<Dictionary<string, string>>(cachedItem);
                    }
                    else
                    {
                        redisModel.Status = UI.Resource.RedisKeyNotExsits;
                    }
                }
                catch
                {
                    redisModel.Error = UI.Resource.RedisErrorMessage;
                }
            }
            else
            {
                redisModel.Error = UI.Resource.RedisInvalidKey;
            }

            return redisModel;
        }

        /// <summary>
        /// Refresh RedisCache Async.
        /// </summary>
        /// <param name="key">key of cache item.</param>
        /// <returns>redisModel.</returns>
        [SuppressMessage("Microsoft.Design", "CA1822:Mark members as static", Justification = "Reviewed.")]
        public async Task<RedisModel> RefreshCache(string key)
        {
            var redisModel = new RedisModel();
            var config = new SPOCPIConfiguration();

            if (!string.IsNullOrWhiteSpace(key))
            {
                try
                {
                    CacheManager.GetCacheEntry(Constants.SPOCPI, key, out string cachedItem);
                    if (!string.IsNullOrWhiteSpace(cachedItem))
                    {
                        CacheManager.DeleteCacheEntry(Constants.SPOCPI, key, out string cacheEngine);
                        var dataToAdd = await config.PopulateConfigurationData(key).ConfigureAwait(false);
                        CacheManager.AddCacheEntry(Constants.SPOCPI, key, dataToAdd);
                        CacheManager.GetCacheEntry(Constants.SPOCPI, key, out string cachedEntry);
                        if (!string.IsNullOrWhiteSpace(cachedEntry))
                        {
                            redisModel.Status = UI.Resource.RedisSuccessMessage;
                            redisModel.Data = JsonConvert.DeserializeObject<Dictionary<string, string>>(cachedEntry);
                        }
                    }
                    else
                    {
                        redisModel.Status = UI.Resource.RedisKeyNotExsits;
                    }
                }
                catch
                {
                    redisModel.Error = UI.Resource.RedisErrorMessage;
                }
            }
            else
            {
                redisModel.Error = UI.Resource.RedisInvalidKey;
            }

            return redisModel;
        }
    }
}
