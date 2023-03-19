// <copyright file="CommandLineArgs.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;

    /// <summary> Utility for handling CommandLineArgs passed to the app. </summary>
    public static class CommandLineArgs
    {
        private static readonly List<string> Args = new(Environment.GetCommandLineArgs());

        /// <summary> Try get an argument and its value. </summary>
        /// <param name="arg"> The option to check. </param>
        /// <param name="value"> The value set for the argument, string.empty if no value was set. </param>
        /// <returns> True if the argument exists. </returns>
        public static bool TryGetArgument(string arg, out string value)
        {
            var idx = Args.IndexOf(arg);
            if (idx < 0)
            {
                value = string.Empty;
                return false;
            }

            value = idx < Args.Count - 1 ? Args[idx + 1] : string.Empty;

            return true;
        }

        /// <summary> Checks if the command line argument exists. </summary>
        /// <param name="arg"> The option to check. </param>
        /// <returns> True if it exists. </returns>
        public static bool Contains(string arg)
        {
            return Args.Contains(arg);
        }
    }
}
