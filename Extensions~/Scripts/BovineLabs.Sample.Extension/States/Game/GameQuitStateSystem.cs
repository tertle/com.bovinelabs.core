// <copyright file="GameQuitStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.States.Game
{
    using System.Threading.Tasks;
    using BovineLabs.Core;
    using BovineLabs.Core.App;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Keys;
    using BovineLabs.Core.States;
    using BovineLabs.Sample.Extension.Data;
    using BovineLabs.Sample.Extension.States;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine;

    [UpdateInGroup(typeof(GameStateSystemGroup))]
    public partial struct GameQuitStateSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<GameState, StateQuit, GameStates>(ref state, "quit");
        }

        public void OnStartRunning(ref SystemState state)
        {
            PauseGame.Pause(ref state, true);

            this.DestroyWorld();
        }

        public void OnStopRunning(ref SystemState state)
        {
        }

        private async void DestroyWorld()
        {
            // We can't destroy a world from inside the world update so we yield which calls back outside the world update
            await Task.Yield();

            if (BovineLabsBootstrap.ServiceWorld == null)
            {
                Debug.LogError("Service world not setup");
                return;
            }

            var em = BovineLabsBootstrap.ServiceWorld.EntityManager;
            em.GetSingleton<BLDebug>(false).Debug("GameState set to init");
            var state = (byte)K<GameStates>.NameToKey("init");
            em.SetSingleton(new GameState { Value = new BitArray256 { [state] = true } });
        }

        private struct StateQuit : IComponentData
        {
        }
    }
}
