// <copyright file="ECSTestsFixture.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Testing
{
    using BovineLabs.Core;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;
    using UnityEngine.LowLevel;

    public abstract class ECSTestsFixture
    {
        private bool jobsDebuggerWasEnabled;
        private PlayerLoopSystem previousPlayerLoop;
        private World? previousWorld;

        private World? world;

        protected World World => this.world!;

        protected WorldUnmanaged WorldUnmanaged => this.World!.Unmanaged;

        protected EntityManager Manager { get; private set; }

        protected EntityManager.EntityManagerDebug ManagerDebug { get; private set; }

        protected BlobAssetStore BlobAssetStore { get; private set; }

        [SetUp]
        public virtual void Setup()
        {
            // unit tests preserve the current player loop to restore later, and start from a blank slate.
            this.previousPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());

            this.previousWorld = World.DefaultGameObjectInjectionWorld;
            this.world = World.DefaultGameObjectInjectionWorld = new World("Test World");
            this.World.UpdateAllocatorEnableBlockFree = true;
            this.Manager = this.World.EntityManager;
            this.ManagerDebug = new EntityManager.EntityManagerDebug(this.Manager);

            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled for all tests, and restore the original value at teardown.
            this.jobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            JobsUtility.JobDebuggerEnabled = true;

            BLDebugSystem.Create(this.world);

            this.BlobAssetStore = new BlobAssetStore(128);

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            // In case entities journaling is initialized, clear it
            EntitiesJournaling.Clear();
#endif
        }

        [TearDown]
        public virtual void TearDown()
        {
            this.World.EntityManager.CompleteAllTrackedJobs();

            this.World.DestroyAllSystemsAndLogException(out var errorsWhileDestroyingSystems);
            Assert.IsFalse(errorsWhileDestroyingSystems,
                "One or more exceptions were thrown while destroying systems during test teardown; consult the log for details.");

            this.ManagerDebug.CheckInternalConsistency();

            this.World.Dispose();
            this.world = null;

            World.DefaultGameObjectInjectionWorld = this.previousWorld;
            this.previousWorld = null;

            this.Manager = default;

            this.BlobAssetStore.Dispose();

            JobsUtility.JobDebuggerEnabled = this.jobsDebuggerWasEnabled;

            PlayerLoop.SetPlayerLoop(this.previousPlayerLoop);
        }
    }
}
