// <copyright file="CustomMinuteSchedule.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.WebHooksManager.WebJob
{
    using Shell.SPOCPI.Common;

    /// <summary>
    /// CustomMinuteSchedule is used to configure Timer component for the Web Job.
    /// </summary>
    public sealed class CustomMinuteSchedule : CustomTimerTriggerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMinuteSchedule"/> class.
        /// </summary>
        public CustomMinuteSchedule()
            : base(Resource.WebHooksCustomMinuteSchedule)
        {
        }
    }
}
