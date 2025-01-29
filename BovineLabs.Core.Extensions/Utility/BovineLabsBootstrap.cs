// <copyright file="BovineLabsBootstrap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Scripting;

    /// <summary>
    /// A bootstrap for setting up and handling a separate Service world as well as support for single player and optional multiplayer games via NetCode.
    /// </summary>
    [Preserve]
    [Configurable]
    [SuppressMessage("ReSharper", "RedundantExtendsListEntry", Justification = "Conditional")]
    public abstract partial class BovineLabsBootstrap : ICustomBootstrap
    {
#if !UNITY_SERVER
        [ConfigVar("app.target-frame-rate", 0, "Override the target frame rate, this is in frames per second. If less than or equal to 0 this will be ignored",
            true)]
        private static readonly SharedStatic<int> FrameRate = SharedStatic<int>.GetOrCreate<FrameRateKey>();
#endif

        [ConfigVar("app.fixed-update", 0, "Override the fixed update, this is in frames per second. If less than or equal to 0 this will be ignored", true)]
        private static readonly SharedStatic<int> FixedUpdate = SharedStatic<int>.GetOrCreate<FixedUpdateKey>();

        private World serviceWorld;

#if !UNITY_SERVER
        private World gameWorld;
#endif

        /// <summary> Event for when a game (local, client or server) world is created. Useful for setting up debugging tools. </summary>
        public static event Action<World> GameWorldCreated;

        /// <summary> Gets the service world if it exists. </summary>
        public static World ServiceWorld => Instance.serviceWorld;

#if !UNITY_SERVER
        /// <summary> Gets the single player world if it exists. </summary>
        public static World GameWorld => Instance.gameWorld;
#endif

        /// <summary> Gets the singleton instance of the bootstrap. </summary>
        public static BovineLabsBootstrap Instance { get; private set; }

#if UNITY_NETCODE
        /// <inheritdoc />
        public sealed override bool Initialize(string defaultWorldName)
#else
        [SuppressMessage("ReSharper", "InvocationIsSkipped", Justification = "Conditional")]
        public bool Initialize(string defaultWorldName)
#endif
        {
            Instance = this;

            WorldAllocator.Initialize();

            this.Initialize();
            return true;
        }

        /// <summary> Creates a local game world as long as we aren't a dedicated server. </summary>
        /// <exception cref="InvalidOperationException"> If there is already a local gmae world. </exception>
        public void CreateGameWorld()
        {
#if !UNITY_SERVER
            if (this.gameWorld != null)
            {
                throw new InvalidOperationException("GameWorld has not been correctly cleaned up");
            }

            this.gameWorld = new World("GameWorld", WorldFlags.Game);
            WorldAllocator.CreateAllocator(this.gameWorld.Unmanaged.SequenceNumber);

            World.DefaultGameObjectInjectionWorld = this.gameWorld; // replace default injection world

            var systems = DefaultWorldInitialization.GetAllSystemTypeIndices(WorldSystemFilterFlags.Default);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(this.gameWorld, systems);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(this.gameWorld);

            InitializeWorld(this.gameWorld);
#endif
        }

        /// <summary> Destroys the game world. </summary>
        public void DestroyGameWorld()
        {
#if !UNITY_SERVER
            if (this.gameWorld is not { IsCreated: true })
            {
                return;
            }

            World.DefaultGameObjectInjectionWorld = ServiceWorld;

            DisposeWorld(this.gameWorld);
            this.gameWorld = null;
#endif
        }

        /// <summary> Initialize a world and setup default properties on it. </summary>
        /// <param name="world"> The world to initialize. </param>
        protected static void InitializeWorld(World world)
        {
            if (FixedUpdate.Data > 0)
            {
                var fixedStepSimulationSystemGroup = world.GetExistingSystemManaged<FixedStepSimulationSystemGroup>();
                if (fixedStepSimulationSystemGroup != null)
                {
                    fixedStepSimulationSystemGroup.Timestep = 1f / FixedUpdate.Data;
                }
            }

            GameWorldCreated?.Invoke(world);
        }

        /// <summary> Initialization method for the bootstrap. By default sets up a service world and configures default app settings. </summary>
        protected virtual void Initialize()
        {
            this.CreateServiceWorld();

#if !UNITY_SERVER
            if (FrameRate.Data > 0)
            {
                Application.targetFrameRate = FrameRate.Data;
            }
#endif
        }

        /// <summary> Creates the service world world. </summary>
        /// <exception cref="InvalidOperationException"> If there is already a server world. </exception>
        protected void CreateServiceWorld()
        {
            if (this.serviceWorld != null)
            {
                throw new InvalidOperationException("ServiceWorld has not been correctly cleaned up");
            }

            this.serviceWorld = new World("ServiceWorld", Worlds.ServiceWorld);
            World.DefaultGameObjectInjectionWorld = this.serviceWorld;

            // ServiceWorld
            var systems = DefaultWorldInitialization.GetAllSystemTypeIndices(Worlds.Service);

            // Special case because these exist in Core not extensions so don't have access to Service
            systems.Add(TypeManager.GetSystemTypeIndex<BLDebugSystem>());
#if UNITY_EDITOR || BL_DEBUG
            systems.Add(TypeManager.GetSystemTypeIndex<DebugSystemGroup>());
#endif

            // We find all default systems in the Unity.Entities and add them to the ServiceWorld
            foreach (var systemIndex in TypeManager.GetSystemTypeIndices(WorldSystemFilterFlags.Default).AsArray())
            {
                if (ServiceUnityFilter(systemIndex))
                {
                    systems.Add(systemIndex);
                }
            }

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(this.serviceWorld, systems);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(this.serviceWorld);
        }

        // We find all default systems in the Unity.Entities and add them to the ServiceWorld
        private static bool ServiceUnityFilter(SystemTypeIndex index)
        {
            var t = TypeManager.GetSystemType(index);

            if (t is not { Namespace: not null })
            {
                return false;
            }

            if (t.Namespace.StartsWith("Unity.Scenes"))
            {
                return true;
            }

            if (!t.Namespace.StartsWith("Unity.Entities"))
            {
                return false;
            }

#if UNITY_EDITOR
            if (t.Namespace.Contains("Tests"))
            {
                return false;
            }
#endif

            if (t.Namespace.StartsWith("Unity.Entities.Graphics"))
            {
                return false;
            }

            return t != typeof(FixedStepSimulationSystemGroup) &&
                t != typeof(BeginFixedStepSimulationEntityCommandBufferSystem) &&
                t != typeof(EndFixedStepSimulationEntityCommandBufferSystem) &&
                t != typeof(VariableRateSimulationSystemGroup) &&
                t != typeof(BeginVariableRateSimulationEntityCommandBufferSystem) &&
                t != typeof(EndVariableRateSimulationEntityCommandBufferSystem) &&
                t != typeof(CompanionGameObjectUpdateTransformSystem) &&
#if !BL_DISABLE_TIME
                t != typeof(UpdateWorldTimeSystem) &&
#endif
                t != EntityInternals.CompanionGameObjectUpdateSystemType;

            // TODO do we need transform, companion, fixed/variable update etc
        }

        private static void DisposeWorld(World world)
        {
            var sn = world.SequenceNumber;
            world.Dispose();
            WorldAllocator.DisposeAllocator(sn);
        }

        private struct FrameRateKey
        {
        }

        private struct FixedUpdateKey
        {
        }
    }
}
