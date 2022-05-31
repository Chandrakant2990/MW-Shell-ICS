// -----------------------------------------------------------------------
// <copyright file="ConfigHelper.cs" company="Microsoft Corporation">
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
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The Configuration helper.
    /// </summary>
    public static class ConfigHelper
    {
        /// <summary>
        /// The true string array.
        /// </summary>
        private static string[] trueStringArray = { "TRUE", "ON", "1", "ENABLE", "ENABLED" };

        /// <summary>
        /// The Boolean Reader.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">if set to <c>true</c> [default value].</param>
        /// <returns>The boolean value.</returns>
        public static bool BooleanReader(string key, bool defaultValue)
        {
            string s = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(s))
            {
                return defaultValue;
            }

            if (trueStringArray.Any(s.ToUpperInvariant().Contains))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// The Integer Reader.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>The integer value.</returns>
        public static int IntegerReader(string key, int defaultValue)
        {
            string s = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(s))
            {
                return defaultValue;
            }

            if (int.TryParse(s, out int r))
            {
                return r;
            }

            return defaultValue;
        }

        /// <summary>
        /// The String Reader.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="defaultValue">The default value.</param>
        /// <returns>the string value.</returns>
        public static string StringReader(string key, string defaultValue)
        {
            string s = Environment.GetEnvironmentVariable(key);
            if (string.IsNullOrEmpty(s))
            {
                return defaultValue;
            }

            return s;
        }
    }
}
