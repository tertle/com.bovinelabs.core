// <copyright file="BovineLabsBootstrap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
#if !UNITY_NETCODE
    using System;
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine.Scripting;

    [Preserve]
    [Configurable]
    public abstract class BovineLabsBootstrap : BovineLabsServiceBootstrap
    {
        [ConfigVar("app.fixed-update", 0, "Override the fixed update, this is in frames per second. If less than or equal to 0 this will be ignored", true)]
        private static readonly SharedStatic<int> FixedUpdate = SharedStatic<int>.GetOrCreate<FixedUpdateKey>();

        private static World gameWorld;

        public static World GameWorld => gameWorld;

        protected override void Initialize()
        {
            gameWorld = null;
            base.Initialize();
        }

        public static void CreateGameWorld()
        {
            if (gameWorld != null)
            {
                throw new Exception("GameWorld has not been correctly cleaned up");
            }

            gameWorld = new World("GameWorld", WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = gameWorld; // replace default injection world

            InvokeGameWorldCreated(gameWorld);

            var systems = DefaultWorldInitialization.GetAllSystemTypeIndices(WorldSystemFilterFlags.Default);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(gameWorld, systems);
            ScriptBehaviourUpdateOrder.AppendWorldToCurrentPlayerLoop(gameWorld);

            if (FixedUpdate.Data > 0)
            {
                gameWorld.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().Timestep = 1f / FixedUpdate.Data;
            }
        }

        public static void DestroyGameWorld()
        {
            World.DefaultGameObjectInjectionWorld = ServiceWorld;

            if (gameWorld is not { IsCreated: true })
            {
                return;
            }

            gameWorld.Dispose();
            gameWorld = null;
        }

        private struct FixedUpdateKey
        {
        }
    }
#else
    using System;
    using Unity.Assertions;
    using Unity.Entities;
    using Unity.NetCode;
    using UnityEngine.Scripting;

    [Preserve]
    public class BovineLabsBootstrap : BovineLabsServiceBootstrap
    {
        public static void DestroyGameWorld()
        {
            World.DefaultGameObjectInjectionWorld = ServiceWorld;

            for (var index = ServerWorlds.Count - 1; index >= 0; index--)
            {
                ServerWorlds[index].Dispose();
            }

            for (var index = ClientWorlds.Count - 1; index >= 0; index--)
            {
                ClientWorlds[index].Dispose();
            }

            for (var index = ThinClientWorlds.Count - 1; index >= 0; index--)
            {
                ThinClientWorlds[index].Dispose();
            }
        }

        public static void CreateGameWorld()
        {
            if (ServerWorld != null && ClientWorld != null)
            {
                throw new Exception("ServerWorld has not been correctly cleaned up");
            }

            var requestedPlayType = RequestedPlayType;
            if (requestedPlayType != PlayType.Client)
            {
                CreateServerWorld();
            }

            if (requestedPlayType != PlayType.Server)
            {
                CreateClientWorld();
            }
        }

        public static void CreateServerWorld()
        {
            if (ServerWorld != null)
            {
                throw new Exception("ServerWorld has not been correctly cleaned up");
            }

            ClientServerBootstrap.CreateServerWorld("ServerWorld");
            Assert.IsNotNull(ServerWorld);

            World.DefaultGameObjectInjectionWorld = ServerWorld; // replace default injection world
            InvokeGameWorldCreated(ServerWorld);
        }

        public static void CreateClientWorld()
        {
            if (ClientWorld != null)
            {
                throw new Exception("ServerWorld has not been correctly cleaned up");
            }

            ClientServerBootstrap.CreateClientWorld("ClientWorld");
            Assert.IsNotNull(ClientWorld);

            // Only override default injection if no server world
            if (World.DefaultGameObjectInjectionWorld != ServerWorld)
            {
                World.DefaultGameObjectInjectionWorld = ClientWorld;
            }

            InvokeGameWorldCreated(ClientWorld);
        }

        public static new World CreateServerWorld(string name)
        {
            throw new Exception("Use CreateServerWorld()");
        }


        public static new World CreateClientWorld(string name)
        {
            throw new Exception("Use CreateClientWorld()");
        }
    }
#endif
}
