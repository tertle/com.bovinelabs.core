// <copyright file="ECSTestsFixture.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

// Stripped down version of the Entities.Tests.ECSTextsFixture
namespace BovineLabs.Testing
{
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;
    using UnityEngine.LowLevel;

    public class ECSTestsCommonBase
    {
        [SetUp]
        public virtual void Setup()
        {
        }

        [TearDown]
        public virtual void TearDown()
        {
        }
    }

    public abstract class ECSTestsFixture : ECSTestsCommonBase
    {
        private World previousWorld;
        private PlayerLoopSystem previousPlayerLoop;
        private bool jobsDebuggerWasEnabled;

        protected World World { get; private set; }

        protected EntityManager Manager { get; private set; }

        protected EntityManager.EntityManagerDebug ManagerDebug { get; private set; }

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // unit tests preserve the current player loop to restore later, and start from a blank slate.
            this.previousPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());

            this.previousWorld = World.DefaultGameObjectInjectionWorld;
            this.World = World.DefaultGameObjectInjectionWorld = new World("Test World");
            this.Manager = this.World.EntityManager;
            this.ManagerDebug = new EntityManager.EntityManagerDebug(this.Manager);

            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled for all tests, and restore the original value at teardown.
            this.jobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            JobsUtility.JobDebuggerEnabled = true;
        }

        [TearDown]
        public override void TearDown()
        {
            if (this.World != null && this.World.IsCreated)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                while (this.World.Systems.Count > 0)
                {
                    this.World.DestroySystem(this.World.Systems[0]);
                }

                this.ManagerDebug.CheckInternalConsistency();

                this.World.Dispose();
                this.World = null;

                World.DefaultGameObjectInjectionWorld = this.previousWorld;
                this.previousWorld = null;
                this.Manager = default;
            }

            JobsUtility.JobDebuggerEnabled = this.jobsDebuggerWasEnabled;

            PlayerLoop.SetPlayerLoop(this.previousPlayerLoop);

            base.TearDown();
        }

        public void AssertSameChunk(Entity e0, Entity e1)
        {
            Assert.AreEqual(this.Manager.GetChunk(e0), this.Manager.GetChunk(e1));
        }

        public void AssetHasChangeVersion<T>(Entity e, uint version)
            where T :
#if UNITY_DISABLE_MANAGED_COMPONENTS
        struct,
#endif
        IComponentData
        {
            var type = this.Manager.GetComponentTypeHandle<T>(true);
            var chunk = this.Manager.GetChunk(e);
            Assert.AreEqual(version, chunk.GetChangeVersion(type));
            Assert.IsFalse(chunk.DidChange(type, version));
            Assert.IsTrue(chunk.DidChange(type, version - 1));
        }

        public void AssetHasChunkOrderVersion(Entity e, uint version)
        {
            var chunk = this.Manager.GetChunk(e);
            Assert.AreEqual(version, chunk.GetOrderVersion());
        }

        public void AssetHasBufferChangeVersion<T>(Entity e, uint version) where T : struct, IBufferElementData
        {
            var type = this.Manager.GetBufferTypeHandle<T>(true);
            var chunk = this.Manager.GetChunk(e);
            Assert.AreEqual(version, chunk.GetChangeVersion(type));
            Assert.IsFalse(chunk.DidChange(type, version));
            Assert.IsTrue(chunk.DidChange(type, version - 1));
        }

        public void AssetHasSharedChangeVersion<T>(Entity e, uint version) where T : struct, ISharedComponentData
        {
            var type = this.Manager.GetSharedComponentTypeHandle<T>();
            var chunk = this.Manager.GetChunk(e);
            Assert.AreEqual(version, chunk.GetChangeVersion(type));
            Assert.IsFalse(chunk.DidChange(type, version));
            Assert.IsTrue(chunk.DidChange(type, version - 1));
        }
    }
}
