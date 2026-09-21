namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    public unsafe struct NativeCounter : IDisposable
    {
        [NativeDisableUnsafePtrRestriction]
        private int* _count;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        private AtomicSafetyHandle m_Safety;
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeCounter>();
#endif

        private readonly AllocatorManager.AllocatorHandle _allocator;

        public NativeCounter(AllocatorManager.AllocatorHandle allocator)
        {
            _allocator = allocator;

            _count = CollectionMemory.Allocate<int>(allocator);
            UnsafeUtility.MemClear(_count, UnsafeUtility.SizeOf<int>());

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            CollectionHelper.SetStaticSafetyId<NativeCounter>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public int Increment()
        {
            // Verify that the caller has write permission on this data.
            // This is the race condition protection, without these checks the AtomicSafetyHandle is useless
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            (*_count)++;

            return *_count;
        }

        public int Count
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return *_count;
            }

            set
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                *_count = value;
            }
        }

        public bool IsCreated => _count != null;

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif

            CollectionMemory.Free(_count, _allocator);
            _count = null;
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
            private readonly int* _count;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
            [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
            private readonly AtomicSafetyHandle m_Safety;
#endif

            internal ParallelWriter(NativeCounter counter)
            {
                _count = counter._count;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(counter.m_Safety);
                m_Safety = counter.m_Safety;
                AtomicSafetyHandle.UseSecondaryVersion(ref m_Safety);
#endif
            }

            public int Increment()
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                return Interlocked.Increment(ref *_count);
            }
        }
    }
}
