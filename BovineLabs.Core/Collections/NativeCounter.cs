// <copyright file="NativeCounter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Threading;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    public unsafe struct NativeCounter : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        private int* count;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeCounter>();
#endif

        private readonly AllocatorManager.AllocatorHandle allocator;

        public NativeCounter(AllocatorManager.AllocatorHandle allocator)
        {
            this.allocator = allocator;

            this.count = Memory.Unmanaged.Allocate<int>(allocator);
            UnsafeUtility.MemClear(this.count, UnsafeUtility.SizeOf<int>());

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            CollectionHelper.SetStaticSafetyId<NativeCounter>(ref this.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(this.m_Safety, true);
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

        /// <inheritdoc />
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref this.m_Safety);
#endif

            Memory.Unmanaged.Free(this.count, this.allocator);
            this.count = null;
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
            private readonly AtomicSafetyHandle m_Safety;
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
