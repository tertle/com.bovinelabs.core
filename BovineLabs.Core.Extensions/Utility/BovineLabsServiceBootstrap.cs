// <copyright file="BovineLabsServiceBootstrap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine.Scripting;

    [Preserve]
    [Configurable]
#if UNITY_NETCODE
    public abstract class BovineLabsServiceBootstrap : Unity.NetCode.ClientServerBootstrap
#else
    public abstract class BovineLabsServiceBootstrap : ICustomBootstrap
#endif
    {
        [ConfigVar("app.target-frame-rate", 0, "Override the target frame rate, this is in frames per second. If less than or equal to 0 this will be ignored", true)]
        private static readonly SharedStatic<int> FrameRate = SharedStatic<int>.GetOrCreate<FrameRateKey>();

        private static World serviceWorld;

        public static event Action<World> GameWorldCreated;

        public static World ServiceWorld => serviceWorld;

#if UNITY_NETCODE
        public sealed override bool Initialize(string defaultWorldName)
#else
        public bool Initialize(string defaultWorldName)
#endif
        {
            this.Initialize();
            return true;
        }

        protected virtual void Initialize()
        {
            serviceWorld = new World("ServiceWorld", Worlds.ServiceWorld);
            World.DefaultGameObjectInjectionWorld = serviceWorld;

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

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(serviceWorld, systems);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(serviceWorld);

            if (FrameRate.Data > 0)
            {
                UnityEngine.Application.targetFrameRate = FrameRate.Data;
            }
        }

        protected static void InvokeGameWorldCreated(World world)
        {
            GameWorldCreated?.Invoke(world);
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

        private struct FrameRateKey
        {
        }
    }
}
