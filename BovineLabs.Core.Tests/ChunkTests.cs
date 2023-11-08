// <copyright file="ChunkTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests
{
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;

    public class ChunkTests : ECSTestsFixture
    {
        [Test]
        public void TestChunkComponent()
        {
            var entity1 = Manager.CreateEntity(typeof(TestComponent), ComponentType.ChunkComponent<ChunkComponent>());
            // var query = Manager.CreateEntityQuery(ComponentType.ReadOnly<TestComponent>(), ComponentType.ChunkComponentExclude<ChunkComponent>());
            Manager.SetChunkComponentData(Manager.GetChunk(entity1), new ChunkComponent { Value = 1 });

            var entity2 = Manager.CreateEntity(typeof(TestComponent), ComponentType.ChunkComponent<ChunkComponent>());

            var chunk1 = Manager.GetChunk(entity1);
            var chunk2 = Manager.GetChunk(entity2);

            Assert.AreEqual(chunk1, chunk2);
        }


        private struct TestComponent : IComponentData
        {

        }

        private struct ChunkComponent : IComponentData
        {
            public int Value;
        }
    }
}
