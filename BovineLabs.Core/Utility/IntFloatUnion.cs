// <copyright file="IntFloatUnion.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct IntFloatUnion
    {
        [FieldOffset(0)]
        public int IntValue;

        [FieldOffset(0)]
        public float FloatValue;

        public IntFloatUnion(int value)
        {
            this.FloatValue = 0;
            this.IntValue = value;
        }

        public IntFloatUnion(float value)
        {
            this.IntValue = 0;
            this.FloatValue = value;
        }
    }
}
