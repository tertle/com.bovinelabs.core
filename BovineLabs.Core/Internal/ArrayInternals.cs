// <copyright file="ArrayInternals.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct ArrayInternals
    {
        [NativeDisableUnsafePtrRestriction]
        public void* Buffer;
        public int Length;
    }
}
