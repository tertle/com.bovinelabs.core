namespace BovineLabs.Core.Tests.Utility
{
    using BovineLabs.Core.Utility;
    using BovineLabs.Testing;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Scenes;

    public partial class SubSceneUtilTests : ECSTestsFixture
    {
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void IsSectionPendingUnload_InheritsContainingSectionRequestWithoutChangingOtherSections(int pendingDepth)
        {
            World.GetOrCreateSystem<TestSystem>();
            ref var state = ref WorldUnmanaged.GetExistingSystemState<TestSystem>();
            var root = CreateSection();
            var child = CreateSection(root);
            var grandchild = CreateSection(child);
            var sibling = CreateSection(root);
            var unrelated = CreateSection();
            var sections = new[] { root, child, grandchild };

            foreach (var section in sections)
            {
                Assert.IsFalse(SubSceneUtil.IsSectionPendingUnload(ref state, section));
            }

            Manager.RemoveComponent<RequestSceneLoaded>(sections[pendingDepth]);

            for (var depth = 0; depth < sections.Length; depth++)
            {
                Assert.AreEqual(depth >= pendingDepth, SubSceneUtil.IsSectionPendingUnload(ref state, sections[depth]));
                Assert.AreEqual(depth != pendingDepth, Manager.HasComponent<RequestSceneLoaded>(sections[depth]));
            }

            Assert.AreEqual(pendingDepth == 0, SubSceneUtil.IsSectionPendingUnload(ref state, sibling));
            Assert.IsTrue(Manager.HasComponent<RequestSceneLoaded>(sibling));
            Assert.IsFalse(SubSceneUtil.IsSectionPendingUnload(ref state, unrelated));
            Manager.AddComponent<RequestSceneLoaded>(sections[pendingDepth]);
            foreach (var section in sections)
            {
                Assert.IsFalse(SubSceneUtil.IsSectionPendingUnload(ref state, section));
            }
        }

        private Entity CreateSection(Entity parentSection = default)
        {
            var scene = Manager.CreateEntity(typeof(SceneReference));
            var section = Manager.CreateEntity(typeof(SceneSectionData), typeof(SceneEntityReference), typeof(RequestSceneLoaded));
            Manager.SetComponentData(section, new SceneEntityReference { SceneEntity = scene });
            if (parentSection != Entity.Null)
            {
                var parentTag = new SceneTag { SceneEntity = parentSection };
                Manager.AddSharedComponent(scene, parentTag);
                Manager.AddSharedComponent(section, parentTag);
            }

            return section;
        }

        private partial struct TestSystem : ISystem
        {
        }
    }
}
