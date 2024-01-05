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
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | Worlds.Service)]
    public partial struct PauseSystem : ISystem, ISystemStartStop
    {
        private bool hasPresentation;
        private bool hasPaused;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build();
            state.RequireForUpdate(query);

            this.hasPresentation = state.World.GetExistingSystem<BeginPresentationEntityCommandBufferSystem>() != SystemHandle.Null;
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
            this.hasPaused = true;
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            // NO-OP
        }

        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local", Justification = "SystemAPI")]
        private void SetPaused(ref SystemState state, bool paused)
        {
            SystemAPI.GetSingleton<BLDebug>().Info($"Game Paused: {paused}");

            ref var simulationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<SimulationSystemGroup>();
            simulationSystemGroup.Enabled = !paused;

            // We only pause presentation for initial pause for subscene loading
            // In game pauses should continue to tick presentation
            if (!this.hasPaused && this.hasPresentation)
            {
                ref var presentationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<PresentationSystemGroup>();
                presentationSystemGroup.Enabled = !paused;
            }
        }
    }
}
#endif
