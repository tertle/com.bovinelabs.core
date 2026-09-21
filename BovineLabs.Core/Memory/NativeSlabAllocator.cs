namespace BovineLabs.Core.Memory
{
    using System;
    using BovineLabs.Core.Internal;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    [NativeContainer]
    public unsafe struct NativeSlabAllocator<T> : IDisposable
        where T : unmanaged
    {
        private UnsafeSlabAllocator<T> _slabAllocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        private AtomicSafetyHandle m_Safety;
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeSlabAllocator<T>>();
#endif

        public NativeSlabAllocator(int countPerSlab, AllocatorManager.AllocatorHandle allocator)
        {
            Debug.Assert(countPerSlab > 0);

            _slabAllocator = new UnsafeSlabAllocator<T>(countPerSlab, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionChecks.CheckAllocator(allocator.Handle);

            m_Safety = CollectionHelper.CreateSafetyHandle(allocator.Handle);
            CollectionChecks.InitNativeContainer<T>(m_Safety);

            CollectionHelper.SetStaticSafetyId<NativeSlabAllocator<T>>(ref m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(m_Safety, true);
#endif
        }

        public int AllocationCount
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                return _slabAllocator.AllocationCount;
            }
        }

        public bool IsCreated => _slabAllocator.IsCreated;

        /// <summary>
        /// Returned memory is not cleared.
        /// </summary>
        public T* Alloc()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            return _slabAllocator.Alloc();
        }

        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            _slabAllocator.Clear();
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            _slabAllocator.Dispose();
        }
    }
}
