// <copyright file="NativeThreadStreamEx.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Extensions for NativeThreadStream. </summary>
    public static unsafe class NativeThreadStreamEx
    {
        private static readonly int MaxSize = UnsafeThreadStreamBlockData.AllocationSize - sizeof(void*);

        /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
        /// <param name="writer"> The writer. </param>
        /// <param name="array"> The array to write. </param>
        /// <typeparam name="T"> The type of the array. </typeparam>
        public static void WriteLarge<T>(this ref NativeThreadStream.Writer writer, NativeArray<T> array)
            where T : unmanaged
        {
            var byteArray = array.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
            WriteLarge(ref writer, (byte*)byteArray.GetUnsafeReadOnlyPtr(), byteArray.Length);
        }

        /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
        /// <param name="writer"> The writer. </param>
        /// <param name="data"> The data to write. </param>
        /// <typeparam name="T"> The type of the slice. </typeparam>
        public static void WriteLarge<T>(this ref NativeThreadStream.Writer writer, NativeSlice<T> data)
            where T : unmanaged
        {
            var num = UnsafeUtility.SizeOf<T>();
            var countPerAllocate = MaxSize / num;

            var allocationCount = data.Length / countPerAllocate;
            var allocationRemainder = data.Length % countPerAllocate;

            var maxSize = countPerAllocate * num;
            var maxOffset = data.Stride * countPerAllocate;

            var src = (byte*)data.GetUnsafeReadOnlyPtr();

            // Write the remainder first as this helps avoid an extra allocation most of the time
            // as you'd usually write at minimum the length beforehand
            if (allocationRemainder > 0)
            {
                var dst = writer.Allocate(allocationRemainder * num);
                UnsafeUtility.MemCpyStride(dst, num, src + (allocationCount * maxOffset), data.Stride, num, allocationRemainder);
            }

            for (var i = 0; i < allocationCount; i++)
            {
                var dst = writer.Allocate(maxSize);
                UnsafeUtility.MemCpyStride(dst, num, src + (i * maxOffset), data.Stride, num, countPerAllocate);
            }
        }

        /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
        /// <param name="writer"> The writer. </param>
        /// <param name="data"> The data to write. </param>
        /// <param name="size"> The size of the data. For an array, this is UnsafeUtility.SizeOf{T} * length. </param>
        public static void WriteLarge(this ref NativeThreadStream.Writer writer, byte* data, int size)
        {
            var allocationCount = size / MaxSize;
            var allocationRemainder = size % MaxSize;

            // Write the remainder first as this helps avoid an extra allocation most of the time
            // as you'd usually write at minimum the length beforehand
            if (allocationRemainder > 0)
            {
                var ptr = writer.Allocate(allocationRemainder);
                UnsafeUtility.MemCpy(ptr, data + (allocationCount * MaxSize), allocationRemainder);
            }

            for (var i = 0; i < allocationCount; i++)
            {
                var ptr = writer.Allocate(MaxSize);
                UnsafeUtility.MemCpy(ptr, data + (i * MaxSize), MaxSize);
            }
        }

        /// <summary> Read a chunk of memory that could have been larger than the max allocation size. </summary>
        /// <param name="reader"> The reader. </param>
        /// <param name="buffer"> A buffer to write back to. </param>
        /// <param name="size"> For an array, this is UnsafeUtility.SizeOf{T} * length. </param>
        public static void ReadLarge(this ref NativeThreadStream.Reader reader, byte* buffer, int size)
        {
            var allocationCount = size / MaxSize;
            var allocationRemainder = size % MaxSize;

            // Write the remainder first as this helps avoid an extra chunk allocation most times
            if (allocationRemainder > 0)
            {
                var ptr = reader.ReadUnsafePtr(allocationRemainder);
                UnsafeUtility.MemCpy(buffer + (allocationCount * MaxSize), ptr, allocationRemainder);
            }

            for (var i = 0; i < allocationCount; i++)
            {
                var ptr = reader.ReadUnsafePtr(MaxSize);
                UnsafeUtility.MemCpy(buffer + (i * MaxSize), ptr, MaxSize);
            }
        }

        /// <summary> Read a chunk of memory that could have been larger than the max allocation size. </summary>
        /// <param name="reader"> The reader. </param>
        /// <param name="buffer"> A buffer to write back to. </param>
        /// <param name="length"> The number of elements. </param>
        /// <typeparam name="T"> The element type to read. </typeparam>
        public static void ReadLarge<T>(this ref NativeThreadStream.Reader reader, byte* buffer, int length)
            where T : unmanaged
        {
            var size = sizeof(T) * length;

            var allocationCount = size / MaxSize;
            var allocationRemainder = size % MaxSize;

            // Write the remainder first as this helps avoid an extra chunk allocation most times
            if (allocationRemainder > 0)
            {
                var ptr = reader.ReadUnsafePtr(allocationRemainder);
                UnsafeUtility.MemCpy(buffer + (allocationCount * MaxSize), ptr, allocationRemainder);
            }

            for (var i = 0; i < allocationCount; i++)
            {
                var ptr = reader.ReadUnsafePtr(MaxSize);
                UnsafeUtility.MemCpy(buffer + (i * MaxSize), ptr, MaxSize);
            }
        }
    }
}
