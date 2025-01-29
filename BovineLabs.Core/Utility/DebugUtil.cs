// <copyright file="DebugUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using BovineLabs.Core.Assertions;
    using Unity.Mathematics;

    public static class DebugUtil
    {
        /// <summary> Split a float into integer and decimal for debugging from burst. </summary>
        /// <param name="value"> The value to split. </param>
        /// <param name="digits"> The number of digits to return. e.g. value of 10.1234 and digits 2 will return 10.12. </param>
        /// <param name="integer"> The integer value. </param>
        /// <param name="decimals"> The decimal value. </param>
        public static void SplitInt(float value, int digits, out int integer, out int decimals)
        {
            Check.Assume(digits >= 0);

            var multi = math.pow(10, digits);

            integer = (int)math.trunc(value);
            decimals = (int)(math.frac(value) * multi);
        }

        /// <summary> Split a double into integer and decimal for debugging from burst. </summary>
        /// <param name="value"> The value to split. </param>
        /// <param name="digits"> The number of digits to return. e.g. value of 10.1234 and digits 2 will return 10.12. </param>
        /// <param name="integer"> The integer value. </param>
        /// <param name="decimals"> The decimal value. </param>
        public static void SplitInt(double value, int digits, out int integer, out int decimals)
        {
            Check.Assume(digits >= 0);

            var multi = math.pow(10, digits);

            integer = (int)math.trunc(value);
            decimals = (int)(math.frac(value) * multi);
        }
    }
}
