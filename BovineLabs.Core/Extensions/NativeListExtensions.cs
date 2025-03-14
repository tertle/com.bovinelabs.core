// <copyright file="NativeListExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Threading;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeListExtensions
    {
        public static void ReserveNoResize<T>(this NativeList<T> nativeList, int length, out T* ptr, out int idx)
            where T : unmanaged
        {
            idx = nativeList.Length;
            nativeList.Length += length;
            ptr = (T*)((byte*)nativeList.m_ListData->Ptr + (idx * UnsafeUtility.SizeOf<T>()));
        }

        public static void ReserveNoResize<T>(this NativeList<T>.ParallelWriter nativeList, int length, out T* ptr, out int idx)
            where T : unmanaged
        {
            var newLength = Interlocked.Add(ref nativeList.ListData->m_length, length);
            CheckSufficientCapacity(nativeList.ListData->Capacity, newLength);
            idx = newLength - length;
            ptr = (T*)((byte*)nativeList.Ptr + (idx * UnsafeUtility.SizeOf<T>()));
        }

        public static IntPtr GetUnsafeIntPtr<T>(this NativeList<T> list)
            where T : unmanaged
        {
            return (IntPtr)list.GetUnsafePtr();
        }

        public static IntPtr GetUnsafeReadOnlyIntPtr<T>(this NativeList<T> list)
            where T : unmanaged
        {
            return (IntPtr)list.GetUnsafeReadOnlyPtr();
        }

        public static void Insert<T>(this NativeList<T> list, int index, T item)
            where T : unmanaged
        {
            list.InsertRangeWithBeginEnd(index, index + 1);
            list[index] = item;
        }

        public static void ResizeInitialized<T>(this NativeList<T> list, int length, byte value)
            where T : unmanaged
        {
            list.ResizeUninitialized(length);
            UnsafeUtility.MemSet(list.GetUnsafePtr(), value, UnsafeUtility.SizeOf<int>() * length);
        }

        public static void ResizeInitialized<T>(this NativeList<T> list, int length)
            where T : unmanaged
        {
            list.ResizeUninitialized(length);
            UnsafeUtility.MemClear(list.GetUnsafePtr(), UnsafeUtility.SizeOf<int>() * length);
        }

        public static void AddRange<T>(this NativeList<T> list, T[] array)
            where T : unmanaged
        {
            var gcHandle = GCHandle.Alloc(array, GCHandleType.Pinned);
            list.AddRange((void*)gcHandle.AddrOfPinnedObject(), array.Length);
            gcHandle.Free();
        }

        public static void AddRange<T>(this NativeList<T> list, IEnumerable<T> enumerable)
            where T : unmanaged
        {
            foreach (var e in enumerable)
            {
                list.Add(e);
            }
        }

        public static void ClearAddRange<T>(this NativeList<T> list, IEnumerable<T> enumerable)
            where T : unmanaged
        {
            list.Clear();
            list.AddRange(enumerable);
        }

        public static void ClearAddRange<T>(this NativeList<T> list, NativeArray<T> array)
            where T : unmanaged
        {
            list.Clear();
            list.AddRange(array);
        }

        public static void ClearAddRange<T>(this NativeList<T> list, NativeHashSet<T> hashSet)
            where T : unmanaged, IEquatable<T>
        {
            list.Clear();

            using var se = hashSet.GetEnumerator();
            while (se.MoveNext())
            {
                list.Add(se.Current);
            }
        }

        public static bool Compare<T>(this NativeList<T> list, NativeHashSet<T> hashSet)
            where T : unmanaged, IEquatable<T>
        {
            if (list.Length != hashSet.Count)
            {
                return false;
            }

            foreach (var l in list)
            {
                if (!hashSet.Contains(l))
                {
                    return false;
                }
            }

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
