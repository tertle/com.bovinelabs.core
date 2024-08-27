// <copyright file="GameStateSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Sample.Extension.States
{
    using BovineLabs.Core;
    using BovineLabs.Core.States;
    using BovineLabs.Sample.Extension.Data;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(GameStateSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.Presentation | Worlds.Service)]
    public partial struct GameStateSystem : ISystem, ISystemStartStop
    {
        private StateFlagModel impl;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.impl = new StateFlagModel(ref state, ComponentType.ReadWrite<GameState>(), ComponentType.ReadWrite<GameStatePrevious>());
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            this.impl.Dispose(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            using var ecb = new EntityCommandBuffer(state.WorldUpdateAllocator);
            this.impl.Run(ref state, ecb);
            ecb.Playback(state.EntityManager);
        }
    }
}
