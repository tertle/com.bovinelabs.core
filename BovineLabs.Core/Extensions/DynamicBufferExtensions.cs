// <copyright file="DynamicBufferExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Assertions;
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
            return buffer.AsNativeArray().AsReadOnly();
        }

        /// <summary> Gets a readonly reference to the element at the given index. </summary>
        /// <param name="buffer"> The dynamic buffer to get the element from. </param>
        /// <param name="index"> The zero-based index. </param>
        /// <typeparam name="T"> The buffer type. </typeparam>
        /// <returns> Returns the reference to the element at the index. </returns>
        public static ref readonly T ElementAtRO<T>(this in DynamicBuffer<T> buffer, int index)
            where T : unmanaged
        {
            CheckReadAccess(buffer);
            CheckBounds(buffer, index);
            return ref UnsafeUtility.ArrayElementAsRef<T>(buffer.GetUnsafeReadOnlyPtr(), index);
        }

        /// <summary> Gets an <see langword="unsafe" /> read-only pointer to the contents of the buffer. </summary>
        /// <param name="buffer"> The dynamic buffer to get the element from. </param>
        /// <remarks> This function can only be called in unsafe code contexts. </remarks>
        /// <returns> A typed, unsafe pointer to the first element in the buffer. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void* GetPtr<T>(this DynamicBuffer<T> buffer)
            where T : unmanaged
        {
            var ptr = UnsafeUtility.As<DynamicBuffer<T>, IntPtr>(ref buffer);
            var header = (BufferHeader*)ptr;
            return BufferHeader.GetElementPointer(header);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckReadAccess<T>(this in DynamicBuffer<T> buffer)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(buffer.m_Safety0);
            AtomicSafetyHandle.CheckReadAndThrow(buffer.m_Safety1);
#endif
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CheckWriteAccess<T>(this in DynamicBuffer<T> buffer)
            where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(buffer.m_Safety0);
            AtomicSafetyHandle.CheckWriteAndThrow(buffer.m_Safety1);
            Check.Assume(buffer.m_IsReadOnly == 0);
#endif
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
    }
}
