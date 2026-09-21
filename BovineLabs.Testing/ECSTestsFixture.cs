namespace BovineLabs.Testing
{
    using BovineLabs.Core;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;
    using UnityEngine.LowLevel;

    public abstract class ECSTestsFixture
    {
        private bool _jobsDebuggerWasEnabled;
        private PlayerLoopSystem _previousPlayerLoop;
        private World _previousWorld;

        private World _world;

        protected World World => _world!;

        protected WorldUnmanaged WorldUnmanaged => World!.Unmanaged;

        protected EntityManager Manager { get; private set; }

        protected EntityManager.EntityManagerDebug ManagerDebug { get; private set; }

        protected BlobAssetStore BlobAssetStore { get; private set; }

        protected virtual bool IsEditModeTest => true;

        [SetUp]
        public virtual void Setup()
        {
            // unit tests preserve the current player loop to restore later, and start from a blank slate.
            _previousPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());

            _previousWorld = World.DefaultGameObjectInjectionWorld;
            _world = World.DefaultGameObjectInjectionWorld = new World("Test World", IsEditModeTest ? WorldFlags.Editor : WorldFlags.Game);
            World.UpdateAllocatorEnableBlockFree = true;
            Manager = World.EntityManager;
            ManagerDebug = new EntityManager.EntityManagerDebug(Manager);

            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled for all tests, and restore the original value at teardown.
            _jobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            JobsUtility.JobDebuggerEnabled = true;

            BLDebugSystem.Create(_world);

            BlobAssetStore = new BlobAssetStore(128);

#if UNITY_INCLUDE_INSTRUMENTATION && !DISABLE_ENTITIES_JOURNALING
            // In case entities journaling is initialized, clear it
#pragma warning disable CS0618 // Type or member is obsolete
            EntitiesJournaling.Clear();
#pragma warning restore CS0618 // Type or member is obsolete
#endif
        }

        [TearDown]
        public virtual void TearDown()
        {
            World.EntityManager.CompleteAllTrackedJobs();

            World.DestroyAllSystemsAndLogException(out var errorsWhileDestroyingSystems);
            Assert.IsFalse(errorsWhileDestroyingSystems,
                "One or more exceptions were thrown while destroying systems during test teardown; consult the log for details.");

            ManagerDebug.CheckInternalConsistency();

            World.Dispose();
            _world = null;

            World.DefaultGameObjectInjectionWorld = _previousWorld;
            _previousWorld = null;

            Manager = default;

            BlobAssetStore.Dispose();

            JobsUtility.JobDebuggerEnabled = _jobsDebuggerWasEnabled;

            PlayerLoop.SetPlayerLoop(_previousPlayerLoop);
        }
    }
}
