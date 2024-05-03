// <copyright file="NativeThreadStream.Writer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
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
            public readonly void Write<T>(T value)
                where T : unmanaged
            {
                ref var dst = ref this.Allocate<T>();
                dst = value;
            }

            /// <summary> Allocate space for data. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to the data. </returns>
            public readonly ref T Allocate<T>()
                where T : unmanaged
            {
                CollectionHelper.CheckIsUnmanaged<T>();
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(this.Allocate(size));
            }

            /// <summary> Allocate space for data. </summary>
            /// <param name="size"> Size in bytes. </param>
            /// <returns> Pointer to the data. </returns>
            public readonly byte* Allocate(int size)
            {
                this.CheckAllocateSize(size);
                return this.writer.Allocate(size);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private readonly void CheckAllocateSize(int size)
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
            public readonly void Write(T value)
            {
                ref var dst = ref this.Allocate();
                dst = value;
            }

            /// <summary> Allocate space for data. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to the data. </returns>
            public readonly ref T Allocate()
            {
                CollectionHelper.CheckIsUnmanaged<T>();
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(this.Allocate(size));
            }

            /// <summary> Allocate space for data. </summary>
            /// <param name="size"> Size in bytes. </param>
            /// <returns> Pointer to the data. </returns>
            private readonly byte* Allocate(int size)
            {
                this.CheckAllocateSize(size);
                return this.writer.Allocate(size);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            private readonly void CheckAllocateSize(int size)
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
