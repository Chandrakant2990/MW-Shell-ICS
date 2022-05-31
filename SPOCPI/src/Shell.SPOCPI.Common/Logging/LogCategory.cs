// -----------------------------------------------------------------------
// <copyright file="LogCategory.cs" company="Microsoft Corporation">
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
    /// <summary>
    /// Enumeration for different types of events according to the source/cause.
    /// </summary>
    public enum LogCategory
    {
        /// <summary>
        /// Unknown source
        /// </summary>
        Unknown,

        /// <summary>
        /// Common layer
        /// </summary>
        Common,

        /// <summary>
        /// Notification Receiver Function App
        /// </summary>
        NotificationReceiverFunctionApp,

        /// <summary>
        /// Change Processor Function App
        /// </summary>
        ChangeProcessorFunctionApp,

        /// <summary>
        /// Change Processor WebJob
        /// </summary>
        ChangeProcessorWebJob,

        /// <summary>
        /// Populate Tracking FunctionApp
        /// </summary>
        PopulateTrackingFunctionApp,

        /// <summary>
        /// WebHooks Manager UI
        /// </summary>
        WebHooksManagerUI,

        /// <summary>
        /// WebHooks Manager WebJob
        /// </summary>
        WebHooksManagerWebJob,

        /// <summary>
        /// The refresh delta token web job
        /// </summary>
        RefreshDeltaTokenWebJob,
    }
}
