// <copyright file="CustomTimerTriggerBase.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation
// All rights reserved.
// THIS CODE AND INFORMATION ARE PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
// KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
// IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A
// PARTICULAR PURPOSE.
// </copyright>

namespace Shell.SPOCPI.Common
{
    using System;
    using System.Globalization;
    using Microsoft.Azure.WebJobs.Extensions.Timers;

    /// <summary>
    /// The custom timer trigger base class.
    /// </summary>
    /// <seealso cref="Microsoft.Azure.WebJobs.Extensions.Timers.TimerSchedule" />
    public class CustomTimerTriggerBase : TimerSchedule
    {
        /// <summary>
        /// The timer.
        /// </summary>
        private readonly TimeSpan timer;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomTimerTriggerBase"/> class.
        /// </summary>
        /// <param name="triggerConfigKey">The trigger configuration key.</param>
        public CustomTimerTriggerBase(string triggerConfigKey)
        {
            string timespan = ConfigHelper.StringReader(triggerConfigKey, default(string));
            if (!string.IsNullOrEmpty(timespan))
            {
                this.timer = TimeSpan.Parse(timespan, CultureInfo.InvariantCulture);
            }
            else
            {
                this.timer = TimeSpan.Parse(Resource.CustomTimerValue, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Gets a value indicating whether [adjust for DST].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [adjust for DST]; otherwise, <c>false</c>.
        /// </value>
        public override bool AdjustForDST => false;

        /// <summary>
        /// Gets the next occurrence.
        /// </summary>
        /// <param name="now">The now.</param>
        /// <returns>The next occurrence time of the function.</returns>
        public override DateTime GetNextOccurrence(DateTime now)
        {
            return now.Add(this.timer);
        }
    }
}
