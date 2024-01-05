// <copyright file="BovineLabsBootstrap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Extensions;
    using Unity.Entities;
    using UnityEngine.Scripting;

    [Preserve]
#if UNITY_NETCODE
    public abstract class BovineLabsBootstrap : Unity.Netcode.ClientServerBootstrap
#else
    public abstract class BovineLabsBootstrap : ICustomBootstrap
#endif
    {
        private static World? serviceWorld;
        private static World? gameWorld;

        public static event Action<World>? GameWorldCreated;

        public bool Initialize(string defaultWorldName)
        {
            gameWorld = null;

            serviceWorld = new World("Service World", Worlds.ServiceWorld);
            World.DefaultGameObjectInjectionWorld = serviceWorld;

            // ServiceWorld
            var systems = DefaultWorldInitialization.GetAllSystemTypeIndices(Worlds.Service);

            // Special case because these exist in Core not extensions so don't have access to Service
            systems.Add(TypeManager.GetSystemTypeIndex<BLDebugSystem>());
#if UNITY_EDITOR || BL_DEBUG
            systems.Add(TypeManager.GetSystemTypeIndex<DebugSystemGroup>());
#endif

            // We find all default systems in the Unity.Entities and add them to the ServiceWorld
            var allSystems = TypeManager.GetSystems(WorldSystemFilterFlags.Default)
                .Where(t => t is { Namespace: not null } &&
                            ((t.Namespace.StartsWith("Unity.Entities") && !t.Namespace.StartsWith("Unity.Entities.Graphics")) ||
                             t.Namespace.StartsWith("Unity.Scenes") || t.Namespace.StartsWith("Unity.Transforms")))
                // TODO do we need transform, companion, fixed/variable update etc
#if UNITY_EDITOR
                .Where(s => !s.Namespace!.Contains("Tests"))
#endif
                .Select(TypeManager.GetSystemTypeIndex)
                .ToArray();

            systems.AddRange(allSystems);

            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(serviceWorld, systems);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(serviceWorld);

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
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(gameWorld, systems);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(gameWorld);
        }

        public static void DestroyGameWorld()
        {
            World.DefaultGameObjectInjectionWorld = serviceWorld;

            if (gameWorld == null)
            {
                return;
            }

            gameWorld.Dispose();
            gameWorld = null;
        }
    }
}
