// <copyright file="NativeSlabAllocator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using UnityEngine;

    [NativeContainer]
    public unsafe struct NativeSlabAllocator<T> : IDisposable
        where T : unmanaged
    {
        private UnsafeSlabAllocator<T> slabAllocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#pragma warning disable SA1308
        private AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeSlabAllocator<T>>();
#pragma warning restore SA1308
#endif

        public NativeSlabAllocator(int countPerSlab, AllocatorManager.AllocatorHandle allocator)
        {
            Debug.Assert(countPerSlab > 0);

            this.slabAllocator = new UnsafeSlabAllocator<T>(countPerSlab, allocator);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.CheckAllocator(allocator.Handle);

            this.m_Safety = CollectionHelper.CreateSafetyHandle(allocator.Handle);
            CollectionHelper.InitNativeContainer<T>(this.m_Safety);

            CollectionHelper.SetStaticSafetyId<NativeSlabAllocator<T>>(ref this.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(this.m_Safety, true);
#endif
        }

        public int AllocationCount
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(this.m_Safety);
#endif
                return this.slabAllocator.AllocationCount;
            }
        }

        public bool IsCreated => this.slabAllocator.IsCreated;

        /// <summary> Returns a pointer. This memory is not cleared. </summary>
        /// <returns> The pointer. </returns>
        public T* Alloc()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
            return this.slabAllocator.Alloc();
        }

        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
            this.slabAllocator.Clear();
        }

        /// <inheritdoc />
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref this.m_Safety);
#endif
            this.slabAllocator.Dispose();
        }
    }
}
