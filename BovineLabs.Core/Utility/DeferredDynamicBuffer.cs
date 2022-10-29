// <copyright file="DeferredDynamicBuffer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;

    public unsafe struct DeferredDynamicBuffer<T> : IDisposable
        where T : unmanaged, IBufferElementData
    {
        private readonly UnsafeList<byte>* reference;
        private readonly ComponentType componentType; // TODO use Update in 1.0 if available
        private EntityQuery query;

        public DeferredDynamicBuffer(ComponentSystemBase system, bool isReadOnly = false, bool createSingletonEntity = true)
            : this(ref *system.m_StatePtr, isReadOnly, createSingletonEntity)
        {
        }

        public DeferredDynamicBuffer(ref SystemState system, bool isReadOnly = false, bool createSingletonEntity = true)
        {
            if (createSingletonEntity)
            {
                system.EntityManager.GetOrCreateSingletonEntity<T>();
            }

            this.reference = (UnsafeList<byte>*)UnsafeUtility.Malloc(
                UnsafeUtility.SizeOf<UnsafeList<byte>>(),
                UnsafeUtility.AlignOf<UnsafeList<byte>>(),
                Allocator.Persistent);

            this.componentType = isReadOnly ? ComponentType.ReadOnly<T>() : ComponentType.ReadWrite<T>();
            this.query = system.GetSingletonEntityQueryInternal(this.componentType);
        }

        public void Dispose()
        {
            UnsafeUtility.Free(this.reference, Allocator.Persistent);
        }

        public JobHandle GetDeferredBuffer(ComponentSystemBase system, JobHandle dependency, out NativeArray<T> output)
        {
            return this.GetDeferredBuffer(system.m_StatePtr, dependency, out output);
        }

        /// <summary> Requests a DynamicBuffer as a Deferred NativeArray which will be populated on job schedule. </summary>
        /// <param name="system"></param>
        /// <param name="dependency"></param>
        /// <param name="reference"></param>
        /// <param name="output"> A deferred array of the dynamic Buffer. </param>
        /// <typeparam name="T"> The dynamic Buffer type. </typeparam>
        /// <returns></returns>
        public JobHandle GetDeferredBuffer(SystemState* system, JobHandle dependency, out NativeArray<T> output)
        {
            var buffer = (byte*)this.reference;

            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, AtomicSafetyHandle.Create());
#endif
            var chunks = this.query.ToArchetypeChunkListAsync(Allocator.TempJob, out var handle);
            dependency = JobHandle.CombineDependencies(dependency, handle);

            dependency = new CreateDeferredJob
                {
                    Chunks = chunks.AsDeferredJobArray(),
                    BufferType = system->GetDynamicComponentTypeHandle(this.componentType),
                    Reference = this.reference,
                }
                .Schedule(dependency);

            chunks.Dispose(dependency);

            return dependency;
        }

        public JobHandle GetDeferredBufferWrite(ComponentSystemBase system, JobHandle dependency, out NativeArray<T> output, int length = -1)
        {
            return this.GetDeferredBufferWrite(ref *system.m_StatePtr, dependency, out output, length);
        }

        /// <summary> Requests a DynamicBuffer as a Deferred NativeArray with write permission and optional ability to resize. </summary>
        /// <param name="system"></param>
        /// <param name="dependency"></param>
        /// <param name="reference"></param>
        /// <param name="output"> A deferred array of the dynamic Buffer with write permission. </param>
        /// <param name="length"> Optional length value to resize the buffer to. Note the memory will be uninitialized. </param>
        /// <typeparam name="T"> The dynamic Buffer type. </typeparam>
        /// <returns></returns>
        public JobHandle GetDeferredBufferWrite(ref SystemState system, JobHandle dependency, out NativeArray<T> output, int length = -1)
        {
            var buffer = (byte*)this.reference;

            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, AtomicSafetyHandle.Create());
#endif
            var chunks = system.GetSingletonEntityQueryInternal(ComponentType.ReadWrite<T>()).ToArchetypeChunkListAsync(Allocator.TempJob, out var handle);
            dependency = JobHandle.CombineDependencies(dependency, handle);

            dependency = new ResizeAndCreateDeferredJob
                {
                    Chunks = chunks.AsDeferredJobArray(),
                    BufferType = system.GetDynamicComponentTypeHandle(ComponentType.ReadWrite<T>()),
                    Reference = this.reference,
                    Length = length,
                }
                .Schedule(dependency);

            chunks.Dispose(dependency);

            return dependency;
        }
    }

    [BurstCompile]
    internal unsafe struct CreateDeferredJob : IJob
    {
        [ReadOnly]
        public NativeArray<ArchetypeChunk> Chunks;

        [ReadOnly]
        public DynamicComponentTypeHandle BufferType;

        [NativeDisableUnsafePtrRestriction]
        public UnsafeList<byte>* Reference;

        public void Execute()
        {
            var buffer = this.Chunks[0].GetUntypedBufferAccessor(ref this.BufferType);
            this.Reference->Ptr = (byte*)buffer.GetUnsafeReadOnlyPtr(0);
            this.Reference->m_length = buffer.GetBufferLength(0);
            this.Reference->m_capacity = buffer.GetBufferCapacity(0);
            this.Reference->Allocator = Allocator.None;
        }
    }

    [BurstCompile]
    internal unsafe struct ResizeAndCreateDeferredJob : IJob
    {
        [ReadOnly]
        public NativeArray<ArchetypeChunk> Chunks;

        public DynamicComponentTypeHandle BufferType;

        [NativeDisableUnsafePtrRestriction]
        public UnsafeList<byte>* Reference;

        public int Length;

        public void Execute()
        {
            var buffer = this.Chunks[0].GetUntypedBufferAccessor(ref this.BufferType);

            if (this.Length > 0)
            {
                buffer.ResizeUninitialized(0, this.Length);
            }

            this.Reference->Ptr = (byte*)buffer.GetUnsafeReadOnlyPtr(0);
            this.Reference->m_length = buffer.GetBufferLength(0);
            this.Reference->m_capacity = buffer.GetBufferCapacity(0);
            this.Reference->Allocator = Allocator.None;
        }
    }
}
