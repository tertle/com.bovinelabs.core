// <copyright file="ComponentSystemBaseExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;

    public static class ComponentSystemBaseExtensions
    {
        /// <summary> Requests a DynamicBuffer as a Deferred NativeArray which will be populated on job schedule. </summary>
        /// <param name="system"></param>
        /// <param name="dependency"></param>
        /// <param name="reference"></param>
        /// <param name="output"> A deferred array of the dynamic buffer. </param>
        /// <typeparam name="T"> The dynamic buffer type. </typeparam>
        /// <returns></returns>
        public static unsafe JobHandle GetDeferredBuffer<T>(
            this ComponentSystemBase system,
            JobHandle dependency,
            NativeReference<UnsafeList<byte>> reference,
            out NativeArray<T> output)
            where T : unmanaged, IBufferElementData
        {
            var buffer = (byte*)reference.GetUnsafeReadOnlyPtr();

            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, reference.GetSafetyHandle());
#endif
            var chunks = system.GetSingletonEntityQueryInternal(ComponentType.ReadOnly<T>()).CreateArchetypeChunkArrayAsync(Allocator.TempJob, out var handle);
            dependency = JobHandle.CombineDependencies(dependency, handle);

            dependency = new CreateDeferredJob
                {
                    Chunks = chunks,
                    BufferType = system.GetDynamicComponentTypeHandle(ComponentType.ReadOnly<T>()),
                    Reference = reference,
                }
                .Schedule(dependency);

            return dependency;
        }

        /// <summary> Requests a DynamicBuffer as a Deferred NativeArray with write permission and optional ability to resize. </summary>
        /// <param name="system"></param>
        /// <param name="dependency"></param>
        /// <param name="reference"></param>
        /// <param name="output"> A deferred array of the dynamic buffer with write permission. </param>
        /// <param name="length"></param>
        /// <typeparam name="T"> The dynamic buffer type. </typeparam>
        /// <returns></returns>
        public static unsafe JobHandle GetDeferredBufferWrite<T>(
            this ComponentSystemBase system,
            JobHandle dependency,
            NativeReference<UnsafeList<byte>> reference,
            out NativeArray<T> output,
            int length = -1)
            where T : unmanaged, IBufferElementData
        {
            var buffer = (byte*)reference.GetUnsafeReadOnlyPtr();

            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.Invalid);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, reference.GetSafetyHandle());
#endif
            var chunks = system.GetSingletonEntityQueryInternal(ComponentType.ReadWrite<T>()).CreateArchetypeChunkArrayAsync(Allocator.TempJob, out var handle);
            dependency = JobHandle.CombineDependencies(dependency, handle);

            dependency = new ResizeAndCreateDeferredJob
                {
                    Chunks = chunks,
                    BufferType = system.GetDynamicComponentTypeHandle(ComponentType.ReadWrite<T>()),
                    Reference = reference,
                    Length = length,
                }
                .Schedule(dependency);

            return dependency;
        }

        [BurstCompile]
        private unsafe struct CreateDeferredJob : IJob
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<ArchetypeChunk> Chunks;

            [ReadOnly]
            public DynamicComponentTypeHandle BufferType;

            public NativeReference<UnsafeList<byte>> Reference;

            public void Execute()
            {
                var buffer = this.Chunks[0].GetUntypedBufferAccessor(ref this.BufferType);

                this.Reference.Value = new UnsafeList<byte>
                {
                    Ptr = (byte*)buffer.GetUnsafeReadOnlyPtr(0),
                    m_length = buffer.GetBufferLength(0),
                    m_capacity = buffer.GetBufferCapacity(0),
                    Allocator = Allocator.None,
                };
            }
        }

        [BurstCompile]
        private unsafe struct ResizeAndCreateDeferredJob : IJob
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<ArchetypeChunk> Chunks;

            public DynamicComponentTypeHandle BufferType;

            public NativeReference<UnsafeList<byte>> Reference;
            public int Length;

            public void Execute()
            {
                var buffer = this.Chunks[0].GetUntypedBufferAccessor(ref this.BufferType);

                if (this.Length > 0)
                {
                    buffer.ResizeUninitialized(0, this.Length);
                }

                this.Reference.Value = new UnsafeList<byte>
                {
                    Ptr = (byte*)buffer.GetUnsafeReadOnlyPtr(0),
                    m_length = buffer.GetBufferLength(0),
                    m_capacity = buffer.GetBufferCapacity(0),
                    Allocator = Allocator.None,
                };
            }
        }
    }
}