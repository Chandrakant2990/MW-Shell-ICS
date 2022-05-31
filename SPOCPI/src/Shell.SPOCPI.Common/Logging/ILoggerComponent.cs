// -----------------------------------------------------------------------
// <copyright file="ILoggerComponent.cs" company="Microsoft Corporation">
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

    /// <summary>
    /// Interface for logging to Application Insights.
    /// </summary>
    public interface ILoggerComponent
    {
        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="ex">Logs error with exception.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        void LogError(Exception ex, string applicationName, string moduleName);

        /// <summary>
        /// Logs the error.
        /// </summary>
        /// <param name="ex">Logs error with exception.</param>
        /// <param name="message">Logs error with Message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        void LogError(Exception ex, string message, string applicationName, string moduleName);

        /// <summary>
        /// Logs the information.
        /// </summary>
        /// <param name="message">Logs information message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        void LogInformation(string message, string applicationName, string moduleName);

        /// <summary>
        /// Logs warning messages
        /// </summary>
        /// <param name="message">Logs warning message.</param>
        /// <param name="applicationName">Application name.</param>
        /// <param name="moduleName">Module name.</param>
        void LogWarning(string message, string applicationName, string moduleName);

        /// <summary>
        /// Debug only method which is useful for tracing.
        /// </summary>
        /// <param name="message">>Logs information message.</param>
        void LogDebug(string message);

        /// <summary>
        /// Returns whether logging is enabled.
        /// </summary>
        /// <returns>returns true or false.</returns>
        bool ShouldLog();
    }
}
