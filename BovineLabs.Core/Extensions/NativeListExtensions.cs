// <copyright file="NativeListExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Threading;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeListExtensions
    {
        public static void ReserveNoResize<T>(this NativeList<T>.ParallelWriter nativeList, int length, out T* ptr, out int idx)
            where T : unmanaged
        {
            idx = Interlocked.Add(ref nativeList.ListData->m_length, length) - length;
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
    }
}