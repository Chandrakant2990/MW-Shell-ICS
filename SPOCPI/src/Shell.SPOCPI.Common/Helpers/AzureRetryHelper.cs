// -----------------------------------------------------------------------
// <copyright file="AzureRetryHelper.cs" company="Microsoft Corporation">
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
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Threading;
    using System.Threading.Tasks;
    using Azure;

    /// <summary>
    /// Static helper class that implement retry logic around async operations by running the
    /// operation additional times on transient errors such as a network failure.
    /// </summary>
    public static class AzureRetryHelper
    {
        /// <summary>
        /// Operation retry count.
        /// </summary>
        private static int retryCount = ConfigHelper.IntegerReader(Constants.RetryThresholdCount, 3);

        /// <summary>
        /// Operation retry back off interval (in milliseconds).
        /// </summary>
        private static int retryMilliseconds = ConfigHelper.IntegerReader(Constants.RetryBackoffIntervalTime, 100);

        /// <summary>
        /// Wrapper for the generic method for async operations that don't return a value.
        /// </summary>
        /// <param name="asyncOperation">Async Operation.</param>
        /// <returns>Task status.</returns>
        public static async Task OperationWithBasicRetryAsync(Func<Task> asyncOperation)
        {
            await OperationWithBasicRetryAsync<object>(async () =>
            {
                await asyncOperation().ConfigureAwait(false);
                return string.Empty;
            }).ConfigureAwait(false);
        }

        /// <summary>
        /// Main generic method to perform the supplied async method with multiple retries on transient exceptions/errors.
        /// </summary>
        /// <typeparam name="T">Task type.</typeparam>
        /// <param name="asyncOperation">async operation.</param>
        /// <returns>Task status.</returns>
        public static async Task<T> OperationWithBasicRetryAsync<T>(Func<Task<T>> asyncOperation)
        {
            int currentRetry = 0;

            while (true)
            {
                try
                {
                    return await asyncOperation().ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    currentRetry++;

                    if (currentRetry > retryCount || !IsTransient(ex))
                    {
                        // If this is not a transient error or we should not retry re-throw the exception.
                        throw;
                    }
                }

                // Wait to retry the operation.
                await Task.Delay(retryMilliseconds * currentRetry).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Checks if the provided exception is considered transient in nature or not
        /// Transient include issues such as a single failed network attempt.
        /// </summary>
        /// <param name="originalException">Original exception.</param>
        /// <returns>true if the exception is transient, false otherwise.</returns>
        private static bool IsTransient(Exception originalException)
        {
            // If the exception is an HTTP request exception or timeout or storage related, then assume it is transient
            if (originalException is TimeoutException ||
                originalException is HttpRequestException ||
                originalException is RequestFailedException ||
                originalException is SocketException ||
                originalException is AggregateException)
            {
                return true;
            }

            // Too many requests error - Wait for 1 second before retrying
            if (originalException.Message.Contains("429"))
            {
                Thread.Sleep(1000);
                return true;
            }

            WebException webException = originalException as WebException;
            if (webException != null)
            {
                // If the web exception contains one of the following status values  it may be transient.
                return new[]
                {
                    WebExceptionStatus.ConnectionClosed,
                    WebExceptionStatus.Timeout,
                    WebExceptionStatus.ConnectFailure,
                    WebExceptionStatus.RequestCanceled,
                }.Contains(webException.Status);
            }

            return false;
        }
    }
}
