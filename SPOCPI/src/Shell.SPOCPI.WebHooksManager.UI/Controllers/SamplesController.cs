// -----------------------------------------------------------------------
// <copyright file="SamplesController.cs" company="Microsoft Corporation">
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

    /// <summary>
    /// samples controller view class.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    public class SamplesController : Controller
    {
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns>Action result.</returns>
        [AllowAnonymous]
        public IActionResult Index()
        {
            return this.View();
        }

        /// <summary>
        /// Sample1s this instance.
        /// </summary>
        /// <returns>Action result.</returns>
        [AllowAnonymous]
        public IActionResult Sample1()
        {
            return this.View();
        }

        /// <summary>
        /// Sample2s this instance.
        /// </summary>
        /// <returns>Action result.</returns>
        [AllowAnonymous]
        public IActionResult Sample2()
        {
            return this.View();
        }

        /// <summary>
        /// Sample3s this instance.
        /// </summary>
        /// <returns>Action result.</returns>
        [AllowAnonymous]
        public IActionResult Sample3()
        {
            return this.View();
        }
    }
}