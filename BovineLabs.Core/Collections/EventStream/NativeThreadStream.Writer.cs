// <copyright file="NativeThreadStream.Writer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe partial struct NativeThreadStream
    {
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public readonly struct Writer
        {
            private readonly UnsafeThreadStream.Writer writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal readonly AtomicSafetyHandle m_Safety;
            internal static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<Writer>();
#endif

            internal Writer(ref NativeThreadStream stream)
            {
                this.writer = stream.stream.AsWriter();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                this.m_Safety = stream.m_Safety;
                CollectionHelper.SetStaticSafetyId(ref this.m_Safety, ref s_staticSafetyId.Data, "BovineLabs.Core.Collections.NativeThreadStream.Writer");
#endif
            }

            /// <summary> Write data to the stream. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <param name="value"> The value to write. </param>
            public void Write<T>(T value)
                where T : unmanaged
            {
                ref var dst = ref this.Allocate<T>();
                dst = value;
            }

            /// <summary> Allocate space for data. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to the data. </returns>
            public ref T Allocate<T>()
                where T : unmanaged
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(this.Allocate(size));
            }

            /// <summary> Allocate space for data. </summary>
            /// <param name="size"> Size in bytes. </param>
            /// <returns> Pointer to the data. </returns>
            public byte* Allocate(int size)
            {
                this.CheckAllocateSize(size);
                return this.writer.Allocate(size);
            }

            /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
            /// <param name="array"> The array to write. </param>
            /// <typeparam name="T"> The type of the array. </typeparam>
            public void WriteLarge<T>(NativeArray<T> array)
                where T : unmanaged
            {
                var byteArray = array.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
                this.WriteLarge((byte*)byteArray.GetUnsafeReadOnlyPtr(), byteArray.Length);
            }

            /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
            /// <param name="data"> The data to write. </param>
            /// <typeparam name="T"> The type of the slice. </typeparam>
            public void WriteLarge<T>(NativeSlice<T> data)
                where T : unmanaged
            {
                var num = UnsafeUtility.SizeOf<T>();
                var countPerAllocate = MaxLargeSize / num;

                var allocationCount = data.Length / countPerAllocate;
                var allocationRemainder = data.Length % countPerAllocate;

                var maxSize = countPerAllocate * num;
                var maxOffset = data.Stride * countPerAllocate;

                var src = (byte*)data.GetUnsafeReadOnlyPtr();

                // Write the remainder first as this helps avoid an extra allocation most of the time
                // as you'd usually write at minimum the length beforehand
                if (allocationRemainder > 0)
                {
                    var dst = this.Allocate(allocationRemainder * num);
                    UnsafeUtility.MemCpyStride(dst, num, src + (allocationCount * maxOffset), data.Stride, num, allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var dst = this.Allocate(maxSize);
                    UnsafeUtility.MemCpyStride(dst, num, src + (i * maxOffset), data.Stride, num, countPerAllocate);
                }
            }

            /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
            /// <param name="data"> The data to write. </param>
            /// <param name="size"> The size of the data. For an array, this is UnsafeUtility.SizeOf{T} * length. </param>
            public void WriteLarge(byte* data, int size)
            {
                var allocationCount = size / MaxLargeSize;
                var allocationRemainder = size % MaxLargeSize;

                // Write the remainder first as this helps avoid an extra allocation most of the time
                // as you'd usually write at minimum the length beforehand
                if (allocationRemainder > 0)
                {
                    var ptr = this.Allocate(allocationRemainder);
                    UnsafeUtility.MemCpy(ptr, data + (allocationCount * MaxLargeSize), allocationRemainder);
                }

                for (var i = 0; i < allocationCount; i++)
                {
                    var ptr = this.Allocate(MaxLargeSize);
                    UnsafeUtility.MemCpy(ptr, data + (i * MaxLargeSize), MaxLargeSize);
                }
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckAllocateSize(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);

                if (size > UnsafeThreadStreamBlockData.AllocationSize - sizeof(void*))
                {
                    throw new ArgumentException("Allocation size is too large");
                }
#endif
            }
        }

        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public readonly struct Writer<T>
            where T : unmanaged
        {
            private readonly UnsafeThreadStream.Writer writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#pragma warning disable SA1308
            private readonly AtomicSafetyHandle m_Safety;
#pragma warning restore SA1308
#endif

            internal Writer(ref NativeThreadStream stream)
            {
                this.writer = stream.stream.AsWriter();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                this.m_Safety = stream.m_Safety;
#endif
            }

            /// <summary> Write data to the stream. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <param name="value"> The value to write. </param>
            public void Write(T value)
            {
                ref var dst = ref this.Allocate();
                dst = value;
            }

            /// <summary> Allocate space for data. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to the data. </returns>
            public ref T Allocate()
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(this.Allocate(size));
            }

            /// <summary> Allocate space for data. </summary>
            /// <param name="size"> Size in bytes. </param>
            /// <returns> Pointer to the data. </returns>
            private byte* Allocate(int size)
            {
                this.CheckAllocateSize(size);
                return this.writer.Allocate(size);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private void CheckAllocateSize(int size)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);

                if (size > UnsafeThreadStreamBlockData.AllocationSize - sizeof(void*))
                {
                    throw new ArgumentException("Allocation size is too large");
                }
#endif
            }
        }
    }
}
