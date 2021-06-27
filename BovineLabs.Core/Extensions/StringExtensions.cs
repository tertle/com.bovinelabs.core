// <copyright file="StringExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Linq;

    /// <summary> Extensions for strings. </summary>
    public static class StringExtensions
    {
        /// <summary> Splits a PascalCase or CamelCase string into a sentence. </summary>
        /// <param name="input"> The string to split. </param>
        /// <returns> The returned sentence. </returns>
        public static string ToSentence(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var output = string.Empty;

            for (var index = 0; index < input.Length; index++)
            {
                // Check neighbours to support acronyms
                if (index > 0 && index < input.Length - 1)
                {
                    if (char.IsUpper(input[index]) && (!char.IsUpper(input[index - 1]) || !char.IsUpper(input[index + 1])))
                    {
                        output += $" {input[index]}";
                        continue;
                    }
                }

                output += input[index];
            }

            return output;
        }

        /// <summary> Capitalize the first character in a string.. </summary>
        /// <param name="input"> The string to capitalize. </param>
        /// <returns> The string with first character capitalized. </returns>
        public static string FirstCharToUpper(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return input.First().ToString().ToUpper() + input.Substring(1);
        }

        /// <summary> Trim a string from the end of a string. </summary>
        /// <param name="source"> The source string. </param>
        /// <param name="value"> The string to trim. </param>
        /// <returns> The trimmed string. </returns>
        /// <example> source=TestString, value=String, result=Test. </example>
        public static string TrimEnd(this string source, string value)
        {
            if (!source.EndsWith(value))
            {
                return source;
            }

            return source.Remove(source.LastIndexOf(value, StringComparison.Ordinal));
        }
    }
}