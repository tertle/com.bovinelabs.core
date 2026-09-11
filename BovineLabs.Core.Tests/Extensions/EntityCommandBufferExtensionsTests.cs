// <copyright file="EntityCommandBufferExtensionsTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Extensions
{
    using System;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Jobs;

    public unsafe class EntityCommandBufferExtensionsTests : ECSTestsFixture
    {
        [Test]
        public void AddUntypedBuffer_ParallelAndMainThreadRecording_PlayBackEveryBuffer()
        {
            const int count = 8192;
            using var entities = this.Manager.CreateEntity(this.Manager.CreateArchetype(), count, Allocator.TempJob);
            using var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var mainThreadEntity = this.Manager.CreateEntity();
            var mainThreadBuffer = commandBuffer.AsParallelWriter().AddUntypedBuffer(-1, mainThreadEntity, ComponentType.ReadWrite<TestBuffer>());
            mainThreadBuffer.ResizeUninitialized(1);
            *(TestBuffer*)mainThreadBuffer.GetUnsafePtr() = new TestBuffer { Value = -1 };

            new RecordBuffersJob
            {
                Entities = entities,
                CommandBuffer = commandBuffer.AsParallelWriter(),
            }.Schedule(count, 1).Complete();

            commandBuffer.Playback(this.Manager);

            Assert.AreEqual(-1, this.Manager.GetBuffer<TestBuffer>(mainThreadEntity)[0].Value);
            for (var index = 0; index < count; index++)
            {
                var buffer = this.Manager.GetBuffer<TestBuffer>(entities[index]);
                Assert.AreEqual((index % 8) + 1, buffer.Length, $"Entity {index}");
                for (var element = 0; element < buffer.Length; element++)
                {
                    Assert.AreEqual((index * 8) + element, buffer[element].Value, $"Entity {index}, element {element}");
                }
            }
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        public void AddUntypedBuffer_AfterPlayback_InvalidatesRecordedBuffer()
        {
            using var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var entity = this.Manager.CreateEntity();
            var buffer = commandBuffer.AsParallelWriter().AddUntypedBuffer(0, entity, ComponentType.ReadWrite<TestBuffer>());
            buffer.ResizeUninitialized(1);

            commandBuffer.Playback(this.Manager);

            Assert.Throws<ObjectDisposedException>(() => buffer.ResizeUninitialized(2));
        }

        [Test]
        public void AddUntypedBuffer_WhileWriterIsScheduled_RejectsMainThreadWrite()
        {
            using var entities = this.Manager.CreateEntity(this.Manager.CreateArchetype(), 1, Allocator.TempJob);
            using var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var mainThreadEntity = this.Manager.CreateEntity();
            var writer = commandBuffer.AsParallelWriter();
            var dependency = new RecordBuffersJob
            {
                Entities = entities,
                CommandBuffer = writer,
            }.Schedule(1, 1);

            try
            {
                Assert.Throws<InvalidOperationException>(() => writer.AddUntypedBuffer(0, mainThreadEntity, ComponentType.ReadWrite<TestBuffer>()));
            }
            finally
            {
                dependency.Complete();
            }
        }
#endif

        [BurstCompile]
        private struct RecordBuffersJob : IJobParallelFor
        {
            [ReadOnly]
            public NativeArray<Entity> Entities;

            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public void Execute(int index)
            {
                var buffer = this.CommandBuffer.AddUntypedBuffer(index, this.Entities[index], ComponentType.ReadWrite<TestBuffer>());
                buffer.ResizeUninitialized((index % 8) + 1);
                var elements = (TestBuffer*)buffer.GetUnsafePtr();
                for (var element = 0; element < buffer.Length; element++)
                {
                    elements[element] = new TestBuffer { Value = (index * 8) + element };
                }
            }
        }

        [InternalBufferCapacity(0)]
        private struct TestBuffer : IBufferElementData
        {
            public int Value;
        }
    }
}
