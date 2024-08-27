// <copyright file="GameInitStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.States.Game
{
    using BovineLabs.Core.App;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.States;
    using BovineLabs.Sample.Extension;
    using BovineLabs.Sample.Extension.Data;
    using BovineLabs.Sample.Extension.States;
    using Unity.Burst;
    using Unity.Entities;

    [Configurable]
    [UpdateInGroup(typeof(GameStateSystemGroup))]
    public partial struct GameInitStateSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<GameState, StateInit, GameStates>(ref state, "init");
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.QueryBuilder().WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build().IsEmptyIgnoreFilter)
            {
                return;
            }

            GameAPI.StateSet(ref state, "game");
        }

        private struct StateInit : IComponentData
        {
        }
    }
}
