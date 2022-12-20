// <copyright file="ECSTestsFixture.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Testing
{
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;
    using UnityEngine.LowLevel;

    public abstract class ECSTestsFixture : ECSTestsCommonBase
    {
        private bool jobsDebuggerWasEnabled;
        private PlayerLoopSystem previousPlayerLoop;
        private World previousWorld;

        protected World World { get; private set; }

        protected WorldUnmanaged WorldUnmanaged => this.World.Unmanaged;

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
            this.World.UpdateAllocatorEnableBlockFree = true;
            this.Manager = this.World.EntityManager;
            this.ManagerDebug = new EntityManager.EntityManagerDebug(this.Manager);

            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled for all tests, and restore the original value at teardown.
            this.jobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            JobsUtility.JobDebuggerEnabled = true;

            // JobsUtility.ClearSystemIds();

#if (UNITY_EDITOR || DEVELOPMENT_BUILD) && !DISABLE_ENTITIES_JOURNALING
            // In case entities journaling is initialized, clear it
            EntitiesJournaling.Clear();
#endif
        }

        [TearDown]
        public override void TearDown()
        {
            if ((this.World != null) && this.World.IsCreated)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                while (this.World.Systems.Count > 0)
                {
                    this.World.DestroySystemManaged(this.World.Systems[0]);
                }

                this.ManagerDebug.CheckInternalConsistency();

                this.World.Dispose();
                this.World = null;

                World.DefaultGameObjectInjectionWorld = this.previousWorld;
                this.previousWorld = null;
                this.Manager = default;
            }

            JobsUtility.JobDebuggerEnabled = this.jobsDebuggerWasEnabled;

            // JobsUtility.ClearSystemIds();

#if !UNITY_DOTSRUNTIME
            PlayerLoop.SetPlayerLoop(this.previousPlayerLoop);
#endif

            base.TearDown();
        }
    }
}
