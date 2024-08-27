// <copyright file="ServiceMenuStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.States.Service
{
    using BovineLabs.Core;
    using BovineLabs.Core.States;
    using BovineLabs.Sample.Extension;
    using BovineLabs.Sample.Extension.Data;
    using BovineLabs.Sample.Extension.States;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(GameStateSystemGroup))]
    [WorldSystemFilter(Worlds.Service)]
    public partial struct ServiceMenuStateSystem : ISystem, ISystemStartStop
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<GameState, StateMenu, GameStates>(ref state, "menu");
        }

        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            GameAPI.UISet(ref state, "menu");
        }

        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            GameAPI.UIDisable(ref state, "menu");
        }

        private struct StateMenu : IComponentData
        {
        }
    }
}
