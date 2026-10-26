namespace BovineLabs.Testing
{
    using System.Collections.Generic;
    using BovineLabs.Core;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Jobs.LowLevel.Unsafe;
    using UnityEngine.LowLevel;

    public abstract class ECSTestsFixture
    {
        private readonly List<World> _additionalWorlds = new();

        private bool _jobsDebuggerWasEnabled;
        private PlayerLoopSystem _previousPlayerLoop;

        private World _world;

        protected World World => _world!;

        protected WorldUnmanaged WorldUnmanaged => World!.Unmanaged;

        protected EntityManager Manager { get; private set; }

        protected EntityManager.EntityManagerDebug ManagerDebug { get; private set; }

        protected BlobAssetStore BlobAssetStore { get; private set; }

        protected virtual bool IsEditModeTest => true;

        /// <summary> Gets the flags of the test world. Override for role-specific worlds such as <see cref="WorldFlags.GameServer"/>. </summary>
        protected virtual WorldFlags TestWorldFlags => IsEditModeTest ? WorldFlags.Editor : WorldFlags.Game;

        /// <summary> Gets a value indicating whether the Jobs Debugger is forced on. Benchmarks return false to keep the Editor's setting. </summary>
        protected virtual bool ForceJobsDebugger => true;

        [SetUp]
        public virtual void Setup()
        {
            // unit tests preserve the current player loop to restore later, and start from a blank slate.
            _previousPlayerLoop = PlayerLoop.GetCurrentPlayerLoop();
            PlayerLoop.SetPlayerLoop(PlayerLoop.GetDefaultPlayerLoop());

            _world = new World("Test World", TestWorldFlags);
            World.PushWorldStack(_world);
            World.UpdateAllocatorEnableBlockFree = true;
            Manager = World.EntityManager;
            ManagerDebug = new EntityManager.EntityManagerDebug(Manager);

            // Many ECS tests will only pass if the Jobs Debugger enabled;
            // force it enabled unless a benchmark opts out, and restore the original value at teardown.
            _jobsDebuggerWasEnabled = JobsUtility.JobDebuggerEnabled;
            if (ForceJobsDebugger)
            {
                JobsUtility.JobDebuggerEnabled = true;
            }

            BLDebugSystem.Create(_world);

            BlobAssetStore = new BlobAssetStore(128);
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Clear first so a failed check cannot leave disposed worlds for the next test.
            var additionalWorlds = _additionalWorlds.ToArray();
            _additionalWorlds.Clear();
            for (var i = additionalWorlds.Length - 1; i >= 0; i--)
            {
                DestroyWorld(additionalWorlds[i]);
            }

            World.EntityManager.CompleteAllTrackedJobs();

            World.DestroyAllSystemsAndLogException(out var errorsWhileDestroyingSystems);
            Assert.IsFalse(errorsWhileDestroyingSystems,
                "One or more exceptions were thrown while destroying systems during test teardown; consult the log for details.");

            ManagerDebug.CheckInternalConsistency();

            World.PopWorldStack(_world);
            World.Dispose();
            _world = null;

            Manager = default;

            BlobAssetStore.Dispose();

            JobsUtility.JobDebuggerEnabled = _jobsDebuggerWasEnabled;

            PlayerLoop.SetPlayerLoop(_previousPlayerLoop);
        }

        /// <summary>
        /// Creates an empty world beside the test world. Unlike the test world it is not pushed on the world stack and has no Core debug state,
        /// so serialization and transfer tests see only their own entities. The fixture destroys additional worlds in reverse order before the
        /// test world, with the same teardown checks.
        /// </summary>
        protected World CreateWorld(string name, WorldFlags flags = WorldFlags.Simulation)
        {
            var world = new World(name, flags);
            world.UpdateAllocatorEnableBlockFree = true;
            _additionalWorlds.Add(world);
            return world;
        }

        /// <summary> Adds a world created elsewhere, such as by <c>ClientServerBootstrap</c>, to the fixture's additional worlds. </summary>
        protected World AddWorld(World world)
        {
            _additionalWorlds.Add(world);
            return world;
        }

        /// <summary> Destroys an additional world before teardown, with the teardown checks. </summary>
        protected void DisposeWorld(World world)
        {
            Assert.IsTrue(_additionalWorlds.Remove(world), $"{world.Name} is not an additional world of the fixture.");
            DestroyWorld(world);
        }

        private static void DestroyWorld(World world)
        {
            world.EntityManager.CompleteAllTrackedJobs();

            world.DestroyAllSystemsAndLogException(out var errorsWhileDestroyingSystems);
            Assert.IsFalse(errorsWhileDestroyingSystems,
                $"One or more exceptions were thrown while destroying the systems of {world.Name}; consult the log for details.");

            new EntityManager.EntityManagerDebug(world.EntityManager).CheckInternalConsistency();

            ScriptBehaviourUpdateOrder.RemoveWorldFromCurrentPlayerLoop(world);
            world.Dispose();
        }
    }
}
