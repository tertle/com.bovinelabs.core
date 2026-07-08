// <copyright file="WorldObjectRegistryCreateDestroySystemTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_CORE_EXTENSIONS && !BL_DISABLE_OBJECT_DEFINITION && !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.Tests.ObjectManagement
{
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.LifeCycle;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;

    public class WorldObjectRegistryCreateDestroySystemTests : ECSTestsFixture
    {
        [Test]
        public void RegistrySystem_OnCreate_CreatesInitializedSingleton()
        {
            this.World.CreateSystem<WorldObjectRegistryCreateSystem>();

            var registry = this.GetRegistry();

            Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void InitializeEntity_RegistersLiveObject()
        {
            var createSystem = this.World.CreateSystem<WorldObjectRegistryCreateSystem>();
            var objectId = new ObjectId(11);
            var entity = this.CreateInitializingObject(objectId, typeof(InitializeEntity));

            createSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.GetRegistry().TryGetFirstValue(objectId, out var registered, out _));
            Assert.AreEqual(entity, registered);
        }

        [Test]
        public void InitializeSubSceneEntity_RegistersLiveObject()
        {
            var createSystem = this.World.CreateSystem<WorldObjectRegistryCreateSystem>();
            var objectId = new ObjectId(12);
            var entity = this.CreateInitializingObject(objectId, typeof(InitializeSubSceneEntity));

            createSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            Assert.IsTrue(this.GetRegistry().TryGetFirstValue(objectId, out var registered, out _));
            Assert.AreEqual(entity, registered);
        }

        [Test]
        public void InitializeEntity_AllowsDuplicateLiveObjects()
        {
            var createSystem = this.World.CreateSystem<WorldObjectRegistryCreateSystem>();
            var objectId = new ObjectId(13);
            var first = this.CreateInitializingObject(objectId, typeof(InitializeEntity));
            var second = this.CreateInitializingObject(objectId, typeof(InitializeEntity));

            createSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var values = this.GetRegistry().GetValuesForKey(objectId);
            var count = 0;
            while (values.MoveNext())
            {
                if (values.Current == first || values.Current == second)
                {
                    count++;
                }
            }

            Assert.AreEqual(2, count);
        }

        [Test]
        public void DestroyEntity_UnregistersMatchingLiveObject()
        {
            var createSystem = this.World.CreateSystem<WorldObjectRegistryCreateSystem>();
            var destroySystem = this.World.CreateSystem<WorldObjectRegistryDestroySystem>();
            var objectId = new ObjectId(14);
            var first = this.CreateInitializingObject(objectId, typeof(InitializeEntity));
            var second = this.CreateInitializingObject(objectId, typeof(InitializeEntity));

            createSystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();
            this.Manager.SetComponentEnabled<InitializeEntity>(first, false);
            this.Manager.SetComponentEnabled<InitializeEntity>(second, false);
            this.Manager.SetComponentEnabled<DestroyEntity>(first, true);

            destroySystem.Update(this.WorldUnmanaged);
            this.Manager.CompleteAllTrackedJobs();

            var values = this.GetRegistry().GetValuesForKey(objectId);
            var foundFirst = false;
            var foundSecond = false;
            while (values.MoveNext())
            {
                foundFirst |= values.Current == first;
                foundSecond |= values.Current == second;
            }

            Assert.IsFalse(foundFirst);
            Assert.IsTrue(foundSecond);
        }

        private DynamicMultiHashMap<ObjectId, Entity> GetRegistry()
        {
            using var query = this.Manager.CreateEntityQuery(typeof(WorldObjectRegistry));
            Assert.AreEqual(1, query.CalculateEntityCount());

            return this.Manager.GetBuffer<WorldObjectRegistry>(query.GetSingletonEntity()).AsMultiHashMap<WorldObjectRegistry, ObjectId, Entity>();
        }

        private Entity CreateInitializingObject(ObjectId objectId, ComponentType initializeType)
        {
            var entity = this.CreateObject(objectId, initializeType);

            this.Manager.SetComponentEnabled(entity, initializeType, true);

            return entity;
        }

        private Entity CreateObject(ObjectId objectId, ComponentType initializeType = default)
        {
            var objectIdType = ComponentType.ReadWrite<ObjectId>();
            var destroyType = ComponentType.ReadWrite<DestroyEntity>();
            var entity = initializeType == default
                ? this.Manager.CreateEntity(objectIdType, destroyType)
                : this.Manager.CreateEntity(objectIdType, destroyType, initializeType);

            this.Manager.SetComponentData(entity, objectId);
            this.Manager.SetComponentEnabled<DestroyEntity>(entity, false);

            return entity;
        }
    }
}
#endif
