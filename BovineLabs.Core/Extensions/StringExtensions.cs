// <copyright file="StringExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Text;

    /// <summary> Extensions for strings. </summary>
    public static class StringExtensions
    {
        /// <summary> Splits a PascalCase or camelCase string into a sentence. </summary>
        /// <param name="input"> The string to split. </param>
        /// <returns> The returned sentence. </returns>
        public static string ToSentence(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            var output = new StringBuilder();

            for (var index = 0; index < input.Length; index++)
            {
                // Check neighbours to support acronyms
                if (index > 0 && index < input.Length - 1)
                {
                    if (char.IsUpper(input[index]) && (!char.IsUpper(input[index - 1]) || !char.IsUpper(input[index + 1])))
                    {
                        output.Append(' ');
                        output.Append(input[index]);
                        continue;
                    }
                }

                output.Append(input[index]);
            }

            return output.ToString();
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

            return char.ToUpper(input[0]) + input[1..];
        }

        /// <summary> Lower case the first character in a string. </summary>
        /// <param name="input"> The string to modify. </param>
        /// <returns> The string with first character in lower case. </returns>
        public static string FirstCharToLower(this string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return input;
            }

            return char.ToLower(input[0]) + input[1..];
        }

        /// <summary> Trim a string from the start of a string. </summary>
        /// <param name="source"> The source string. </param>
        /// <param name="value"> The string to trim. </param>
        /// <returns> The trimmed string. </returns>
        /// <example> source=TestString, value=String, result=Test. </example>
        public static string TrimStart(this string source, string value)
        {
            if (!source.StartsWith(value))
            {
                return source;
            }

            return source.Remove(source.IndexOf(value, StringComparison.Ordinal), value.Length);
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
