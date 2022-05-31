// -----------------------------------------------------------------------
// <copyright file="HomeController.cs" company="Microsoft Corporation">
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
    using System.Diagnostics;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Shell.SPOCPI.WebHooksManager.UI.Models;
    using Constants = Shell.SPOCPI.Common.Constants;

    /// <summary>
    /// Home Controller.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    [Authorize(Roles = Constants.AdminAndOperatorRole)]
    public class HomeController : Controller
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
        /// About this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        [Authorize]
        public IActionResult About()
        {
            this.ViewData[Resource.Message] = Resource.ApplicationDescription;

            return this.View();
        }

        /// <summary>
        /// Contacts this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        [AllowAnonymous]
        public IActionResult Contact()
        {
            this.ViewData[Resource.Message] = Resource.ApplicationContact;

            return this.View();
        }

        /// <summary>
        /// Privacies this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return this.View();
        }

        /// <summary>
        /// Documentations this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        [AllowAnonymous]
        public IActionResult Documentation()
        {
            return this.View();
        }

        /// <summary>
        /// Errors this instance.
        /// </summary>
        /// <returns>Action Result.</returns>
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return this.View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? this.HttpContext.TraceIdentifier });
        }
    }
}
