// -----------------------------------------------------------------------
// <copyright file="AccountController.cs" company="Microsoft Corporation">
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
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Authentication.OpenIdConnect;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    /// <summary>
    /// Account Controller view.
    /// </summary>
    /// <seealso cref="Microsoft.AspNetCore.Mvc.Controller" />
    public class AccountController : Controller
    {
        /// <summary>
        /// Access Denied.
        /// </summary>
        /// <returns>Access Denied view.</returns>
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return this.View();
        }

        /// <summary>
        /// SignOut.
        /// </summary>
        /// <returns>SignOut user.</returns>
        [HttpGet]
        public IActionResult SignOut()
        {
            var callbackUrl = this.Url.Action("SignedOut", "Account", values: null, protocol: this.Request.Scheme);
            return this.SignOut(
                new AuthenticationProperties { RedirectUri = callbackUrl },
                CookieAuthenticationDefaults.AuthenticationScheme,
                OpenIdConnectDefaults.AuthenticationScheme);
        }

        /// <summary>
        /// SignedOut.
        /// </summary>
        /// <returns>SignedOut user.</returns>
        [HttpGet]
        public IActionResult SignedOut()
        {
            if (this.User.Identity.IsAuthenticated)
            {
                // Redirect to home page if the user is authenticated.
                return this.RedirectToAction(nameof(HomeController.Index), "Home");
            }

            return this.RedirectToAction(nameof(HomeController.Index), string.Empty);
        }
    }
}