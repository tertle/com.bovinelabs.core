// <copyright file="NativeArrayFactory.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Creates owned native arrays from jobs without Unity's temporary-allocation-only safety check. </summary>
    /// <typeparam name="T"> The unmanaged element type. </typeparam>
    public static unsafe class NativeArrayFactory<T>
        where T : unmanaged
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly SharedStatic<int> StaticSafetyId = SharedStatic<int>.GetOrCreate<NativeArray<T>>();
#endif

        /// <summary> Creates an owned native array from a job. </summary>
        /// <param name="length"> The number of elements. </param>
        /// <param name="allocator"> The allocator used for the owned memory. </param>
        /// <param name="options"> Whether to clear the allocated memory. </param>
        /// <returns> An array that must be disposed by its owner. </returns>
        public static NativeArray<T> CreateFromJob(
            int length, Allocator allocator, NativeArrayOptions options = NativeArrayOptions.ClearMemory)
        {
            var size = UnsafeUtility.SizeOf<T>() * (long)length;
            CheckAllocateArguments(length, allocator);

            var buffer = UnsafeUtility.MallocTracked(size, UnsafeUtility.AlignOf<T>(), allocator, 0);
            var array = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, length, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safety = CollectionHelper.CreateSafetyHandle(allocator);

            if (UnsafeUtility.IsNativeContainerType<T>())
            {
                AtomicSafetyHandle.SetNestedContainer(safety, true);
            }

            CollectionHelper.SetStaticSafetyId<NativeArray<T>>(ref safety, ref StaticSafetyId.Data);
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref array, safety);
#endif

            if ((options & NativeArrayOptions.ClearMemory) == NativeArrayOptions.ClearMemory)
            {
                UnsafeUtility.MemClear(buffer, size);
            }

            return array;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckAllocateArguments(int length, Allocator allocator)
        {
            if (allocator <= Allocator.None)
            {
                throw new ArgumentException("Allocator must be Temp, TempJob or Persistent", nameof(allocator));
            }

            if (allocator >= Allocator.FirstUserIndex)
            {
                throw new ArgumentException(
                    "Use CollectionHelper.CreateNativeArray in com.unity.collections package for custom allocator", nameof(allocator));
            }

            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), "Length must be >= 0");
            }
        }
    }
}
