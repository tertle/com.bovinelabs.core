namespace BovineLabs.Core.Tests.Extensions
{
    using System;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public class EntityManagerInstantiateTests : ECSTestsFixture
    {
        [Test]
        public void Instantiate_CreatesClonesInChunksOfRequestedValue()
        {
            var prefab = CreatePrefab(1);
            Manager.SetComponentData(prefab, new InstantiateData { Value = 7 });

            var existing = Manager.CreateEntity(typeof(InstantiateShared), typeof(InstantiateData));
            Manager.SetSharedComponent(existing, new InstantiateShared { Value = 2 });

            using var instances = new NativeArray<Entity>(3, Allocator.Temp);
            Manager.Instantiate(prefab, instances, new InstantiateShared { Value = 2 });

            foreach (var instance in instances)
            {
                Assert.AreEqual(2, Manager.GetSharedComponent<InstantiateShared>(instance).Value);
                Assert.AreEqual(7, Manager.GetComponentData<InstantiateData>(instance).Value);
                Assert.IsFalse(Manager.HasComponent<Prefab>(instance));
                Assert.AreEqual(Manager.GetChunk(existing), Manager.GetChunk(instance));
            }

            Assert.AreEqual(1, Manager.GetSharedComponent<InstantiateShared>(prefab).Value);
        }

        [Test]
        public void Instantiate_SingleEntity_UsesRequestedValue()
        {
            var prefab = CreatePrefab(1);

            var instance = Manager.Instantiate(prefab, new InstantiateShared { Value = 4 });

            Assert.AreEqual(4, Manager.GetSharedComponent<InstantiateShared>(instance).Value);
            Assert.IsFalse(Manager.HasComponent<Prefab>(instance));
        }

        [Test]
        public void Instantiate_SetsValueOnLinkedEntitiesThatHaveTheType()
        {
            var root = CreatePrefab(1);
            Manager.AddComponent<InstantiateReference>(root);

            var sharedChild = CreatePrefab(1);
            var plainChild = Manager.CreateEntity(typeof(InstantiateData), typeof(Prefab));

            Manager.SetComponentData(root, new InstantiateReference { Value = sharedChild });
            var group = Manager.AddBuffer<LinkedEntityGroup>(root);
            group.Add(root);
            group.Add(sharedChild);
            group.Add(plainChild);

            var instance = Manager.Instantiate(root, new InstantiateShared { Value = 5 });

            var instanceGroup = Manager.GetBuffer<LinkedEntityGroup>(instance);
            Assert.AreEqual(3, instanceGroup.Length);
            Assert.AreEqual(instance, instanceGroup[0].Value);

            var instanceSharedChild = instanceGroup[1].Value;
            var instancePlainChild = instanceGroup[2].Value;
            Assert.AreEqual(5, Manager.GetSharedComponent<InstantiateShared>(instance).Value);
            Assert.AreEqual(5, Manager.GetSharedComponent<InstantiateShared>(instanceSharedChild).Value);
            Assert.IsFalse(Manager.HasComponent<InstantiateShared>(instancePlainChild));
            Assert.IsTrue(Manager.HasComponent<InstantiateData>(instancePlainChild));
            Assert.IsFalse(Manager.HasComponent<Prefab>(instancePlainChild));

            // Entity references inside the group are remapped to the clone, as with EntityManager.Instantiate.
            Assert.AreEqual(instanceSharedChild, Manager.GetComponentData<InstantiateReference>(instance).Value);
        }

        [Test]
        public void Instantiate_RootWithoutTypeSetsValueOnChildren()
        {
            var root = Manager.CreateEntity(typeof(InstantiateData), typeof(Prefab));
            var child = CreatePrefab(1);
            var group = Manager.AddBuffer<LinkedEntityGroup>(root);
            group.Add(root);
            group.Add(child);

            var instance = Manager.Instantiate(root, new InstantiateShared { Value = 6 });

            var instanceChild = Manager.GetBuffer<LinkedEntityGroup>(instance)[1].Value;
            Assert.IsFalse(Manager.HasComponent<InstantiateShared>(instance));
            Assert.AreEqual(6, Manager.GetSharedComponent<InstantiateShared>(instanceChild).Value);
        }

        [Test]
        public void Instantiate_NoEntityHasType_Throws()
        {
            var single = Manager.CreateEntity(typeof(InstantiateData), typeof(Prefab));
            Assert.Throws<ArgumentException>(() => Manager.Instantiate(single, new InstantiateShared { Value = 1 }));

            var root = Manager.CreateEntity(typeof(InstantiateData), typeof(Prefab));
            var child = Manager.CreateEntity(typeof(InstantiateData), typeof(Prefab));
            var group = Manager.AddBuffer<LinkedEntityGroup>(root);
            group.Add(root);
            group.Add(child);

            using var instances = new NativeArray<Entity>(2, Allocator.Temp);
            Assert.Throws<ArgumentException>(() => Manager.Instantiate(root, instances, new InstantiateShared { Value = 1 }));

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<InstantiateData>().Build(Manager);
            Assert.AreEqual(0, query.CalculateEntityCount());
        }

        [Test]
        public void Instantiate_FillsSeveralChunks()
        {
            var prefab = CreatePrefab(1);
            var capacity = Manager.CreateArchetype(typeof(InstantiateShared), typeof(InstantiateData)).ChunkCapacity;
            var count = (capacity * 2) + 1;

            using var instances = new NativeArray<Entity>(count, Allocator.Temp);
            Manager.Instantiate(prefab, instances, new InstantiateShared { Value = 3 });

            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<InstantiateShared, InstantiateData>().Build(Manager);
            query.SetSharedComponentFilter(new InstantiateShared { Value = 3 });
            Assert.AreEqual(count, query.CalculateEntityCount());
            Assert.AreEqual(3, query.CalculateChunkCount());

            using var unique = new NativeHashSet<Entity>(count, Allocator.Temp);
            foreach (var instance in instances)
            {
                Assert.IsTrue(unique.Add(instance));
                Assert.AreEqual(3, Manager.GetSharedComponent<InstantiateShared>(instance).Value);
            }
        }

        [Test]
        public void Instantiate_ReleasesValueWithLastInstance()
        {
            var prefab = CreatePrefab(1);

            using var instances = new NativeArray<Entity>(2, Allocator.Temp);
            Manager.Instantiate(prefab, instances, new InstantiateShared { Value = 9 });
            Assert.IsTrue(HasValue(9));

            Manager.DestroyEntity(instances);
            Assert.IsFalse(HasValue(9));
        }

        [Test]
        public void Instantiate_NoInstances_KeepsNoValue()
        {
            var prefab = CreatePrefab(1);

            using var instances = new NativeArray<Entity>(0, Allocator.Temp);
            Manager.Instantiate(prefab, instances, new InstantiateShared { Value = 9 });

            Assert.IsFalse(HasValue(9));
        }

        private Entity CreatePrefab(int value)
        {
            var prefab = Manager.CreateEntity(typeof(InstantiateShared), typeof(InstantiateData), typeof(Prefab));
            Manager.SetSharedComponent(prefab, new InstantiateShared { Value = value });
            return prefab;
        }

        private bool HasValue(int value)
        {
            Manager.GetAllUniqueSharedComponents<InstantiateShared>(out var values, Allocator.Temp);
            var found = false;
            foreach (var shared in values)
            {
                found |= shared.Value == value;
            }

            values.Dispose();
            return found;
        }

        private struct InstantiateShared : ISharedComponentData
        {
            public int Value;
        }

        private struct InstantiateData : IComponentData
        {
            public int Value;
        }

        private struct InstantiateReference : IComponentData
        {
            public Entity Value;
        }
    }
}
