// <copyright file="ComponentSystemBaseExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Extensions
{
    using BovineLabs.Basics.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Jobs;

    public static class ComponentSystemBaseExtensions
    {
        public static unsafe JobHandle GetDeferredBuffer<T>(
            this ComponentSystemBase system,
            JobHandle dependency,
            NativeReference<UnsafeList> reference,
            out NativeArray<T> output)
            where T : struct, IBufferElementData
        {
            var entity = system.GetSingletonEntity<T>();

            var buffer = (byte*)reference.GetUnsafeReadOnlyPtr();

            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, reference.GetSafetyHandle());
#endif

            dependency = new CreateDeferredJob<T>
                {
                    Entity = entity,
                    Buffers = system.GetBufferFromEntity<T>(true),
                    Reference = reference,
                }
                .Schedule(dependency);

            return dependency;
        }

        public static unsafe JobHandle GetDeferredBufferWrite<T>(
            this ComponentSystemBase system,
            JobHandle dependency,
            NativeReference<UnsafeList> reference,
            out NativeArray<T> output,
            int length = -1)
            where T : struct, IBufferElementData
        {
            var entity = system.GetSingletonEntity<T>();

            var buffer = (byte*)reference.GetUnsafeReadOnlyPtr();

            // We use the first bit of the pointer to infer that the array is in list mode
            // Thus the job scheduling code will need to patch it.
            buffer += 1;
            output = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<T>(buffer, 0, Allocator.None);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref output, reference.GetSafetyHandle());
#endif

            dependency = new ResizeAndCreateDeferredJob<T>
                {
                    Entity = entity,
                    Buffers = system.GetBufferFromEntity<T>(),
                    Reference = reference,
                    Length = length,
                }
                .Schedule(dependency);

            return dependency;
        }

        [BurstCompile]
        private unsafe struct CreateDeferredJob<T> : IJob
            where T : struct, IBufferElementData
        {
            // This can be read or write
            public BufferFromEntity<T> Buffers;

            public Entity Entity;
            public NativeReference<UnsafeList> Reference;

            public void Execute()
            {
                var array = this.Buffers[this.Entity].AsNativeArray();

                this.Reference.Value = new UnsafeList
                {
                    Ptr = array.GetUnsafeReadOnlyPtr(),
                    Length = array.Length,
                    Capacity = array.Length,
                    Allocator = Allocator.None,
                };
            }
        }

        [BurstCompile]
        private unsafe struct ResizeAndCreateDeferredJob<T> : IJob
            where T : struct, IBufferElementData
        {
            // This can be read or write
            public BufferFromEntity<T> Buffers;

            public Entity Entity;
            public NativeReference<UnsafeList> Reference;
            public int Length;

            public void Execute()
            {
                var buffer = this.Buffers[this.Entity];

                if (this.Length > 0)
                {
                    buffer.ResizeUninitialized(this.Length);
                }

                var array = buffer.AsNativeArray();

                this.Reference.Value = new UnsafeList
                {
                    Ptr = array.GetUnsafeReadOnlyPtr(),
                    Length = array.Length,
                    Capacity = array.Length,
                    Allocator = Allocator.None,
                };
            }
        }
    }
}