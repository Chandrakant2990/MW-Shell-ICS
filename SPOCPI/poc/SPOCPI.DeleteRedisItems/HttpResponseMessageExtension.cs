// <copyright file="HttpResponseMessageExtension.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace SPOCPI.DeleteRedisItems
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;

    /// <summary>
    /// The Http Response Message Extension class.
    /// </summary>
    public static class HttpResponseMessageExtension
    {
        /// <summary>
        /// Exceptions the response.
        /// </summary>
        /// <param name="httpResponseMessage">The HTTP response message.</param>
        /// <returns>The exception string.</returns>
        public static async Task<string> ExceptionResponse(this HttpResponseMessage httpResponseMessage)
        {
            if (httpResponseMessage is null)
            {
                throw new ArgumentNullException(nameof(httpResponseMessage));
            }

            var errorMessage = await httpResponseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
            return $"StatusCode : {httpResponseMessage.StatusCode} | {Constants.ErrorMessage} : {errorMessage}";
        }
    }
}