// <copyright file="NativeSliceExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System.Runtime.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeSliceExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ElementAt<T>(this NativeSlice<T> array, int index)
            where T : unmanaged
        {
            return ref ReadArrayElementWithStrideRef<T>(array.GetUnsafePtr(), index, array.Stride);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ref T ReadArrayElementWithStrideRef<T>(void* source, int index, int stride)
            where T : unmanaged
        {
            return ref UnsafeUtility.AsRef<T>((byte*)source + (index * (long)stride));
        }
    }
}
