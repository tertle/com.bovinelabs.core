namespace BovineLabs.Testing
{
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;
    using UnityEngine.LowLevel;

    public abstract class ECSTestsFixture : ECSTestsCommonBase
    {
        protected World PreviousWorld;
        protected World World;
        protected PlayerLoopSystem PreviousPlayerLoop;
        protected EntityManager Manager;
        protected EntityManager.EntityManagerDebug ManagerDebug;

        protected int StressTestEntityCount = 1000;
        private bool JobsDebuggerWasEnabled;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            // unit tests preserve the current player loop to restore later, and start from a blank slate.
            this.PreviousPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());

            this.PreviousWorld = World.DefaultGameObjectInjectionWorld;
            this.World = World.DefaultGameObjectInjectionWorld = new World("Test World");
            this.World.UpdateAllocatorEnableBlockFree = true;
            this.Manager = this.World.EntityManager;
            this.ManagerDebug = new EntityManager.EntityManagerDebug(this.Manager);

            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled for all tests, and restore the original value at teardown.
            this.JobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
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
            if (this.World != null && this.World.IsCreated)
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

                World.DefaultGameObjectInjectionWorld = this.PreviousWorld;
                this.PreviousWorld = null;
                this.Manager = default;
            }

            JobsUtility.JobDebuggerEnabled = this.JobsDebuggerWasEnabled;

            // JobsUtility.ClearSystemIds();

#if !UNITY_DOTSRUNTIME
            PlayerLoop.SetPlayerLoop(this.PreviousPlayerLoop);
#endif

            base.TearDown();
        }
    }
}
