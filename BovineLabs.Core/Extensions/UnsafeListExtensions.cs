// <copyright file="UnsafeListExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class UnsafeListExtensions
    {
        public static void ReserveNoResize<T>(ref this UnsafeList<T> list, int length, out T* ptr, out int idx)
            where T : unmanaged
        {
            idx = list.Length;
            list.Length += length;
            ptr = (T*)((byte*)list.Ptr + (idx * UnsafeUtility.SizeOf<T>()));
        }
    }
}
