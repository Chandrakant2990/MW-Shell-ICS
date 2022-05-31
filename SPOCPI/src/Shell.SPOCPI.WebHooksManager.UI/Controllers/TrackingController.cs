// -----------------------------------------------------------------------
// <copyright file="TrackingController.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>
//-----------------------------------------------------------------------

namespace Shell.SPOCPI.WebHooksManager.UI.Controllers
{
    using System;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Primitives;
    using Shell.SPOCPI.Common;
    using Constants = Shell.SPOCPI.Common.Constants;

    /// <summary>
    /// Tracking Controller view.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    [Authorize(Roles = Constants.AdminAndOperatorRole)]
    public class TrackingController : Controller
    {
        /// <summary>
        /// The logger component.
        /// </summary>
        private readonly ILoggerComponent loggerComponent;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackingController" /> class.
        /// </summary>
        /// <param name="logger">The logger component.</param>
        public TrackingController(ILoggerComponent logger)
        {
            this.loggerComponent = logger;
        }

        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        public IActionResult Index()
        {
            try
            {
                this.Request.Query.TryGetValue(UI.Resource.SmallDriveId, out StringValues parameters);
                this.ViewData[UI.Resource.DriveId] = parameters.ToString();
            }
            catch (Exception ex)
            {
                this.loggerComponent.LogError(ex, Constants.SPOCPI, nameof(LogCategory.WebHooksManagerUI));
            }

            return this.View();
        }
    }
}