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
            idx = Interlocked.Add(ref nativeList.ListData->Length, length) - length;
            ptr = (T*)((byte*)nativeList.Ptr + (idx * UnsafeUtility.SizeOf<T>()));
        }

        public static IntPtr GetUnsafeIntPtr<T>(this NativeList<T> list)
            where T : struct
        {
            return (IntPtr)list.GetUnsafePtr();
        }

        public static IntPtr GetUnsafeReadOnlyIntPtr<T>(this NativeList<T> list)
            where T : struct
        {
            return (IntPtr)list.GetUnsafeReadOnlyPtr();
        }
    }
}