// <copyright file="BoolExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Runtime.InteropServices;

    /// <summary> Extensions for bool. </summary>
    public static class BoolExtensions
    {
        /// <summary> Remap a bool as a byte without conditions for brachless algorithms. </summary>
        /// <param name="value"> The bool value. </param>
        /// <returns> Either 1 for true, or 0 for false. </returns>
        public static byte AsByte(this bool value)
        {
            return new BoolUnion { Condition = value }.Value;
        }

        /// <summary> A union to easily convert byte to bool. </summary>
        [StructLayout(LayoutKind.Explicit)]
        private struct BoolUnion
        {
            [FieldOffset(0)]
            public bool Condition;

            [FieldOffset(0)]
            public readonly byte Value;
        }
    }
}
