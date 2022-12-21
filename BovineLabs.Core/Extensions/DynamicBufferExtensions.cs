// <copyright file="DynamicBufferExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using Unity.Collections;
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

            var elemSize = UnsafeUtility.SizeOf<T>();
            var oldLength = buffer.Length;
            buffer.ResizeUninitialized(oldLength + length);

            var basePtr = (byte*)buffer.GetUnsafePtr();
            UnsafeUtility.MemCpy(basePtr + ((long)oldLength * elemSize), ptr, (long)elemSize * length);
        }

        public static byte* InsertAllocate<T>(this DynamicBuffer<T> buffer, int index, int elementCount)
            where T : unmanaged
        {
            CheckWriteAccess(buffer);
            var length = buffer.Length;
            buffer.ResizeUninitialized(length + elementCount);
            CheckBounds(buffer, index); // CheckBounds after ResizeUninitialized since index == length is allowed
            var elemSize = UnsafeUtility.SizeOf<T>();
            var basePtr = (byte*)buffer.GetUnsafePtr();
            UnsafeUtility.MemMove(basePtr + ((index + elementCount) * elemSize), basePtr + (index * elemSize), (long)elemSize * (length - index));
            return basePtr + (index * elemSize);
        }

        public static NativeArray<T>.ReadOnly AsNativeArrayRO<T>(this in DynamicBuffer<T> buffer)
            where T : unmanaged
        {
            // CheckReadAccess(buffer);
            // TODO implement properly
            return buffer.AsNativeArray().AsReadOnly();
        }

        // return buffer.AsNativeArray().AsReadOnly();
        // fixed (void* ptr = &buffer)
        // {
        //                 var shadow = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(BufferHeader.GetElementPointer((BufferHeader*)ptr), buffer.Length, Allocator.None);
        // #if ENABLE_UNITY_COLLECTIONS_CHECKS
        //                 var handle = buffer.m_Safety1;
        //                 AtomicSafetyHandle.UseSecondaryVersion(ref handle);
        //                 NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref shadow, handle);
        //
        // #endif
        //                 return shadow.AsReadOnly();
        // }
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
        private static void CheckReadAccess<T>(in DynamicBuffer<T> buffer)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(buffer.m_Safety0);
            AtomicSafetyHandle.CheckReadAndThrow(buffer.m_Safety1);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckWriteAccess<T>(in DynamicBuffer<T> buffer)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(buffer.m_Safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(buffer.m_Safety1);
#endif
        }
    }
}
