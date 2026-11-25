namespace BovineLabs.Core.Tests
{
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public class BLDebugSystemTests : ECSTestsFixture
    {
        [Test]
        public void DestroySystem_RemovesEditorLoggersAndPreservesSceneEntities()
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<BLLogger>().Build(Manager);
            var logger = query.GetSingletonEntity();
            var system = World.CreateSystemManaged<BLDebugSystem>();
            var disabledLogger = Manager.CreateEntity(typeof(BLLogger), typeof(Disabled), typeof(LinkedEntityGroup));
            var child = Manager.CreateEntity();
            var linked = Manager.GetBuffer<LinkedEntityGroup>(disabledLogger);
            linked.Add(new LinkedEntityGroup { Value = disabledLogger });
            linked.Add(new LinkedEntityGroup { Value = child });
            var sceneLogger = Manager.CreateEntity(typeof(BLLogger), typeof(SceneTag));

            World.DestroySystemManaged(system);

            Assert.IsFalse(Manager.Exists(logger));
            Assert.IsFalse(Manager.Exists(disabledLogger));
            Assert.IsFalse(Manager.Exists(child));
            Assert.IsTrue(Manager.HasComponent<BLLogger>(sceneLogger));
        }
    }
}
