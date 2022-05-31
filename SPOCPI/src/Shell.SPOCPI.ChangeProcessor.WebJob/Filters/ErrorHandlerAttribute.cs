// <copyright file="ErrorHandlerAttribute.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.ChangeProcessor.WebJob.Filters
{
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs.Host;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Sample exception filter that shows how declarative error handling logic
    /// can be integrated into the execution pipeline.
    /// </summary>
    public sealed class ErrorHandlerAttribute : FunctionExceptionFilterAttribute
    {
        /// <summary>
        /// Called when [exception asynchronous].
        /// </summary>
        /// <param name="exceptionContext">The exception context.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The task status.</returns>
        public override Task OnExceptionAsync(FunctionExceptionContext exceptionContext, CancellationToken cancellationToken)
        {
            if (exceptionContext != null && cancellationToken != null)
            {
                // custom error handling logic could be written here
                // (e.g. write a queue message, send a notification, etc.)
                exceptionContext.Logger.LogError(string.Format(CultureInfo.InvariantCulture, Resource.ErrorHandlerMessage, exceptionContext.FunctionName, exceptionContext.FunctionInstanceId));
            }

            return Task.CompletedTask;
        }
    }
}
