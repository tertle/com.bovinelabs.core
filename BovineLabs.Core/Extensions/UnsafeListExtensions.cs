// <copyright file="UnsafeListExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using Unity.Collections;
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

        public static NativeArray<T> AsArray<T>(this UnsafeList<T> list)
            where T : unmanaged
        {
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(list.Ptr, list.Length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, AtomicSafetyHandle.GetTempMemoryHandle());
#endif
            return array;
        }

        public static int IndexOf<T, TPredicate>(this UnsafeList<T> collection, TPredicate predicate)
            where T : unmanaged
            where TPredicate : IPredicate<T>
        {
            for (var i = 0; i < collection.Length; i++)
            {
                if (predicate.Check(collection[i]))
                {
                    return i;
                }
            }

            return -1;
        }

        public static bool TryGetValue<T, TPredicate>(this UnsafeList<T> collection, TPredicate predicate, out T value)
            where T : unmanaged
            where TPredicate : IPredicate<T>
        {
            var index = collection.IndexOf(predicate);
            if (index == -1)
            {
                value = default;
                return false;
            }

            value = collection[index];
            return true;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckSufficientCapacity(int capacity, int length)
        {
            if (capacity < length)
            {
                throw new InvalidOperationException($"Length {length} exceeds Capacity {capacity}");
            }
        }
    }
}
