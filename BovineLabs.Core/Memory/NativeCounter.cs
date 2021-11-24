// <copyright file="NativeCounter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;
    using System.Threading;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    public unsafe struct NativeCounter : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        private int* count;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;

        [NativeSetClassTypeToNullOnSchedule]
        private DisposeSentinel m_DisposeSentinel;
#endif

        readonly Allocator allocator;

        public NativeCounter(Allocator allocator)
        {
            this.allocator = allocator;

            this.count = Mem.MallocClear<int>(allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out this.m_Safety, out this.m_DisposeSentinel, 0, this.allocator);
#endif
        }

        public int Increment()
        {
            // Verify that the caller has write permission on this data.
            // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
            (*this.count)++;

            return *this.count;
        }

        public int Count
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
                return *this.count;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
                *this.count = value;
            }
        }

        public bool IsCreated => this.count != null;

        /// <inheritdoc/>
        public void Dispose()
        {
            UnsafeUtility.Free(this.count, this.allocator);
            this.count = null;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref this.m_Safety, ref this.m_DisposeSentinel);
#endif
        }

        public ParallelWriter AsParallelWriter()
        {
            return new ParallelWriter(this);
        }

        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly int* count;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle m_Safety;
#endif

            internal ParallelWriter(NativeCounter counter)
            {
                this.count = counter.count;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(counter.m_Safety);
                this.m_Safety = counter.m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref this.m_Safety);
#endif
            }

            public int Increment()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
                return Interlocked.Increment(ref *this.count);
            }
        }
    }
}