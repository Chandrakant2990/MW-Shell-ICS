// <copyright file="UserInputValidation.cs" company="Microsoft Corporation">
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
    using System.Text.RegularExpressions;

    /// <summary>
    /// User Input Validation helper class.
    /// </summary>
    public static class UserInputValidation
    {
        /// <summary>
        /// Checks the valid input.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="pattern">The pattern against which the input will be validated.</param>
        /// <returns>/// <returns>boolean indicating if the input value is valid or not.</returns></returns>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Reviewed.")]
        public static bool CheckValidInput(string input, string pattern)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(pattern))
            {
                throw new ArgumentNullException(nameof(pattern));
            }

            try
            {
                Match match = Regex.Match(input, pattern, RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    //// if matched, means the input string contains HTML tag
                    return false;
                }
                else
                {
                    //// valid input.
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
