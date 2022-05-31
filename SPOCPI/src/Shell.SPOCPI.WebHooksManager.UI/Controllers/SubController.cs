// -----------------------------------------------------------------------
// <copyright file="SubController.cs" company="Microsoft Corporation">
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
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Shell.SPOCPI.Common;
    using Constants = Shell.SPOCPI.Common.Constants;

    /// <summary>
    ///  Subscription controller view.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    [Authorize(Roles = Constants.AdminAndOperatorRole)]
    public class SubController : Controller
    {
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        public ActionResult Index()
        {
            return this.View();
        }

        /// <summary>
        /// Creates this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        [Authorize(Roles = Constants.AdminRole)]
        public ActionResult Create()
        {
            var config = new SPOCPIConfiguration();
            var confidentialSite = config.GetConfigValue(Resource.ConfidentialSite);
            this.ViewData["confidentialSite"] = confidentialSite;
            return this.View();
        }
    }
}