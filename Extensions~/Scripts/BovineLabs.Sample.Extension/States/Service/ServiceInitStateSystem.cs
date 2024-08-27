// <copyright file="ServiceInitStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.States.Service
{
    using BovineLabs.Core;
    using BovineLabs.Core.App;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.States;
    using BovineLabs.Sample.Extension.Data;
    using Unity.Burst;
    using Unity.Entities;

    [Configurable]
    [UpdateInGroup(typeof(GameStateSystemGroup))]
    [WorldSystemFilter(Worlds.Service)]
    public partial struct ServiceInitStateSystem : ISystem
    {
        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            StateAPI.Register<GameState, StateInit, GameStates>(ref state, "init");
        }


        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!SystemAPI.QueryBuilder().WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build().IsEmptyIgnoreFilter)
            {
                return;
            }

            GameAPI.StateSet(ref state, "menu");
        }

        private struct StateInit : IComponentData
        {
        }
    }
}
