// <copyright file="BufferInternals.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct BufferInternals
    {
        [NativeDisableUnsafePtrRestriction]
        public byte* BasePointer;
        public int Length;
        public int Stride;
        public int InternalCapacity;

        public BufferInternals(byte* basePointer, int length, int stride, int internalCapacity)
        {
            this.BasePointer = basePointer;
            this.Length = length;
            this.Stride = stride;
            this.InternalCapacity = internalCapacity;
        }
    }
}
