// <copyright file="GameInStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.States.Game
{
    using BovineLabs.Core.States;
    using BovineLabs.Sample.Extension;
    using BovineLabs.Sample.Extension.Data;
    using BovineLabs.Sample.Extension.States;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(GameStateSystemGroup))]
    public partial struct GameInStateSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<GameState, StateGame, GameStates>(ref state, "game");
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            GameAPI.UISet(ref state, "game");
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            GameAPI.UIDisable(ref state, "game");
        }


        private struct StateGame : IComponentData
        {
        }
    }
}
