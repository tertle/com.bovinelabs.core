// <copyright file="ShortHalfUnion.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Explicit)]
    public struct ShortHalfUnion
    {
        [FieldOffset(0)]
        public short ShortValue;

        [FieldOffset(0)]
        public half HalfValue;

        public ShortHalfUnion(short value)
        {
            this.HalfValue = default;
            this.ShortValue = value;
        }

        public ShortHalfUnion(half value)
        {
            this.ShortValue = default;
            this.HalfValue = value;
        }
    }
}
