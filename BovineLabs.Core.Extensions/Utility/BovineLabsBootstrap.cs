// <copyright file="BovineLabsBootstrap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine.Scripting;
#if !BL_DISABLE_TIME
    using BLUpdateWorldTimeSystem = BovineLabs.Core.Time.UpdateWorldTimeSystem;
    using UnityUpdateWorldTimeSystem = Unity.Entities.UpdateWorldTimeSystem;
#endif

    [Preserve]
    [Configurable]
#if UNITY_NETCODE
    public abstract class BovineLabsBootstrap : Unity.NetCode.ClientServerBootstrap
#else
    public abstract class BovineLabsBootstrap : ICustomBootstrap
#endif
    {
        [ConfigVar("app.fixed-update", 0, "Override the fixed update, this is in frames per second. If less than or equal to 0 this will be ignored", true)]
        private static readonly SharedStatic<int> FixedUpdate = SharedStatic<int>.GetOrCreate<FixedUpdateKey>();

        [ConfigVar("app.target-frame-rate", 0, "Override the target frame rate, this is in frames per second. If less than or equal to 0 this will be ignored", true)]
        private static readonly SharedStatic<int> FrameRate = SharedStatic<int>.GetOrCreate<FrameRateKey>();

        private static World serviceWorld;
        private static World gameWorld;

        public static event Action<World> GameWorldCreated;

        public static World ServiceWorld => serviceWorld;

        public static World GameWorld => gameWorld;

#if UNITY_NETCODE
        public override bool Initialize(string defaultWorldName)
#else
        public bool Initialize(string defaultWorldName)
#endif
        {
            gameWorld = null;

            serviceWorld = new World("Service World", Worlds.ServiceWorld);
            World.DefaultGameObjectInjectionWorld = serviceWorld;

            // ServiceWorld
            var systems = DefaultWorldInitialization.GetAllSystemTypeIndices(Worlds.Service);

            // Special case because these exist in Core not extensions so don't have access to Service
            systems.Add(TypeManager.GetSystemTypeIndex<BLDebugSystem>());
#if !BL_DISABLE_TIME
            systems.Add(TypeManager.GetSystemTypeIndex<BLUpdateWorldTimeSystem>()); // manually created
#endif
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

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(serviceWorld, systems);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(serviceWorld);

            if (FrameRate.Data > 0)
            {
                UnityEngine.Application.targetFrameRate = FrameRate.Data;
            }

            return true;
        }

        public static void CreateGameWorld()
        {
            if (gameWorld != null)
            {
                throw new Exception("GameWorld has not been correctly cleaned up");
            }

            gameWorld = new World("Game World", WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = gameWorld; // replace default injection world

            GameWorldCreated?.Invoke(gameWorld);

            var systems = DefaultWorldInitialization.GetAllSystemTypeIndices(WorldSystemFilterFlags.Default);
#if !BL_DISABLE_TIME
            systems.Add(TypeManager.GetSystemTypeIndex<BLUpdateWorldTimeSystem>());
            Remove<UnityUpdateWorldTimeSystem>(systems);
#endif
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(gameWorld, systems);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(gameWorld);

            if (FixedUpdate.Data > 0)
            {
                gameWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().Timestep = 1f / FixedUpdate.Data;
            }
        }

        public static void DestroyGameWorld()
        {
            World.DefaultGameObjectInjectionWorld = serviceWorld;

            if (gameWorld is not { IsCreated: true })
            {
                return;
            }

            gameWorld.Dispose();
            gameWorld = null;
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
                   t != typeof(UnityUpdateWorldTimeSystem) &&
#endif
                   t != EntityInternals.CompanionGameObjectUpdateSystemType;

            // TODO do we need transform, companion, fixed/variable update etc
        }

        private static void Remove<T>(NativeList<SystemTypeIndex> systems)
        {
            var index = systems.AsArray().IndexOf(new RemoveSystem(TypeManager.GetSystemTypeIndex<T>()));
            if (index == -1)
            {
                return;
            }

            systems.RemoveAtSwapBack(index);
        }

        private struct RemoveSystem : IPredicate<SystemTypeIndex>
        {
            private readonly SystemTypeIndex system;

            public RemoveSystem(SystemTypeIndex system)
            {
                this.system = system;
            }

            public bool Check(SystemTypeIndex other)
            {
                return other == this.system;
            }
        }

        private struct FixedUpdateKey
        {
        }

        private struct FrameRateKey
        {
        }
    }
}