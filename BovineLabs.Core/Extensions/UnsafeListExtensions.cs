// <copyright file="UnsafeListExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Threading;
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


        public static void ReserveNoResize<T>(this UnsafeList<T>.ParallelWriter unsafeList, int length, out T* ptr, out int idx)
            where T : unmanaged
        {
            var newLength = Interlocked.Add(ref unsafeList.ListData->m_length, length);
            CheckSufficientCapacity(unsafeList.ListData->Capacity, newLength);
            idx = newLength - length;
            ptr = (T*)((byte*)unsafeList.Ptr + (idx * UnsafeUtility.SizeOf<T>()));
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS"), Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
            {
                throw new InvalidOperationException($"Length {length} exceeds Capacity {capacity}");
            }
        }
    }
}
