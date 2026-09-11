// <copyright file="SubSceneUtilTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
            this.World.GetOrCreateSystem<TestSystem>();
            ref var state = ref this.WorldUnmanaged.GetExistingSystemState<TestSystem>();
            var root = this.CreateSection();
            var child = this.CreateSection(root);
            var grandchild = this.CreateSection(child);
            var sibling = this.CreateSection(root);
            var unrelated = this.CreateSection();
            var sections = new[] { root, child, grandchild };

            foreach (var section in sections)
            {
                Assert.IsFalse(SubSceneUtil.IsSectionPendingUnload(ref state, section));
            }

            this.Manager.RemoveComponent<RequestSceneLoaded>(sections[pendingDepth]);

            for (var depth = 0; depth < sections.Length; depth++)
            {
                Assert.AreEqual(depth >= pendingDepth, SubSceneUtil.IsSectionPendingUnload(ref state, sections[depth]));
                Assert.AreEqual(depth != pendingDepth, this.Manager.HasComponent<RequestSceneLoaded>(sections[depth]));
            }

            Assert.AreEqual(pendingDepth == 0, SubSceneUtil.IsSectionPendingUnload(ref state, sibling));
            Assert.IsTrue(this.Manager.HasComponent<RequestSceneLoaded>(sibling));
            Assert.IsFalse(SubSceneUtil.IsSectionPendingUnload(ref state, unrelated));
            this.Manager.AddComponent<RequestSceneLoaded>(sections[pendingDepth]);
            foreach (var section in sections)
            {
                Assert.IsFalse(SubSceneUtil.IsSectionPendingUnload(ref state, section));
            }
        }

        private Entity CreateSection(Entity parentSection = default)
        {
            var scene = this.Manager.CreateEntity(typeof(SceneReference));
            var section = this.Manager.CreateEntity(typeof(SceneSectionData), typeof(SceneEntityReference), typeof(RequestSceneLoaded));
            this.Manager.SetComponentData(section, new SceneEntityReference { SceneEntity = scene });
            if (parentSection != Entity.Null)
            {
                var parentTag = new SceneTag { SceneEntity = parentSection };
                this.Manager.AddSharedComponent(scene, parentTag);
                this.Manager.AddSharedComponent(section, parentTag);
            }

            return section;
        }

        private partial struct TestSystem : ISystem
        {
        }
    }
}
