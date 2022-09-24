// <copyright file="DynamicBufferExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class DynamicBufferExtensions
    {
        public static void ResizeInitialized<T>(this DynamicBuffer<T> buffer, int length)
            where T : unmanaged
        {
            CheckWriteAccess(buffer);

            buffer.ResizeUninitialized(length);
            UnsafeUtility.MemClear(buffer.GetUnsafePtr(), UnsafeUtility.SizeOf<T>() * length);
        }

        public static void AddRange<T>(this DynamicBuffer<T> buffer, T* ptr, int length)
            where T : unmanaged
        {
            CheckWriteAccess(buffer);

            int elemSize = UnsafeUtility.SizeOf<T>();
            int oldLength = buffer.Length;
            buffer.ResizeUninitialized(oldLength + length);

            var basePtr = (byte*)buffer.GetUnsafePtr();
            UnsafeUtility.MemCpy(basePtr + ((long)oldLength * elemSize), ptr, (long)elemSize * length);
        }

        public static byte* InsertAllocate<T>(this DynamicBuffer<T> buffer, int index, int elementCount)
            where T : unmanaged
        {
            CheckWriteAccess(buffer);
            int length = buffer.Length;
            buffer.ResizeUninitialized(length + elementCount);
            CheckBounds(buffer, index); // CheckBounds after ResizeUninitialized since index == length is allowed
            int elemSize = UnsafeUtility.SizeOf<T>();
            byte* basePtr = (byte*)buffer.GetUnsafePtr();
            UnsafeUtility.MemMove(basePtr + ((index + elementCount) * elemSize), basePtr + (index * elemSize), (long)elemSize * (length - index));
            return basePtr + (index * elemSize);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckBounds<T>(DynamicBuffer<T> buffer, int index)
            where T : unmanaged
        {
            if ((uint)index >= (uint)buffer.Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range in DynamicBuffer of '{buffer.Length}' Length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckWriteAccess<T>(DynamicBuffer<T> buffer)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(buffer.m_Safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(buffer.m_Safety1);
#endif
        }
    }
}
