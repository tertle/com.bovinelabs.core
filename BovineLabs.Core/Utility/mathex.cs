// <copyright file="mathex.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    public static class mathex
    {
        /// <summary>
        /// Returns the modulus of two numbers unlike % which returns the remainder.
        /// For positive values this is exactly the same as % just slower.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="m"></param>
        /// <returns></returns>
        public static int mod(int x, int m)
        {
            return ((x % m) + m) % m;
        }
    }
}
