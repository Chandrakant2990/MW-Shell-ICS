// -----------------------------------------------------------------------
// <copyright file="TablesController.cs" company="Microsoft Corporation">
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
    using Constants = Shell.SPOCPI.Common.Constants;

    /// <summary>
    /// Tables Controller view.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    [Authorize(Roles = Constants.AdminAndOperatorRole)]
    public class TablesController : Controller
    {
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Index page view.</returns>
        public IActionResult Index()
        {
            return this.View();
        }

        /// <summary>
        /// Configurations this instance.
        /// </summary>
        /// <returns>Configuration table view.</returns>
        public IActionResult Configuration()
        {
            return this.View();
        }

        /// <summary>
        /// View for notification table.
        /// </summary>
        /// <returns>Notification table view.</returns>
        public IActionResult Notification()
        {
            return this.View();
        }
    }
}