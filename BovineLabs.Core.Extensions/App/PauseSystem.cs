// <copyright file="PauseSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.App
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(EndInitializationEntityCommandBufferSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct PauseSystem : ISystem, ISystemStartStop
    {
        private SystemHandle beginSimulationEntityCommandBufferSystem;
        private SystemHandle endSimulationEntityCommandBufferSystem;
        private SystemHandle beginPresentationEntityCommandBufferSystem;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build();
            state.RequireForUpdate(query);

            this.beginSimulationEntityCommandBufferSystem = state.World.GetExistingSystem<BeginSimulationEntityCommandBufferSystem>();
            this.endSimulationEntityCommandBufferSystem = state.World.GetExistingSystem<EndSimulationEntityCommandBufferSystem>();
            this.beginPresentationEntityCommandBufferSystem = state.World.GetExistingSystem<BeginPresentationEntityCommandBufferSystem>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.SetPaused(ref state, true);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            this.SetPaused(ref state, false);
        }

        public void OnUpdate(ref SystemState state)
        {
            // Flush command buffers in case someone is using them to avoid them leaking
            this.beginSimulationEntityCommandBufferSystem.Update(state.WorldUnmanaged);
            this.endSimulationEntityCommandBufferSystem.Update(state.WorldUnmanaged);

            // Server might not have presentation
            if (this.beginPresentationEntityCommandBufferSystem != SystemHandle.Null)
            {
                this.beginPresentationEntityCommandBufferSystem.Update(state.WorldUnmanaged);
            }
        }

        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local", Justification = "SystemAPI")]
        private void SetPaused(ref SystemState state, bool paused)
        {
            SystemAPI.GetSingleton<BLDebug>().Info($"Game Paused: {paused}");

            ref var simulationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<SimulationSystemGroup>();
            simulationSystemGroup.Enabled = !paused;

            if (this.beginPresentationEntityCommandBufferSystem != SystemHandle.Null)
            {
                ref var presentationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<PresentationSystemGroup>();
                presentationSystemGroup.Enabled = !paused;
            }
        }
    }
}
#endif
