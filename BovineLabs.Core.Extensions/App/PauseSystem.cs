// <copyright file="PauseSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.App
{
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Core;
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(EndInitializationEntityCommandBufferSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | Worlds.Service)]
    public partial struct PauseSystem : ISystem, ISystemStartStop
    {
        private EntityQuery query;
        private bool hasPresentation;
        private double pauseTime;

        /// <inheritdoc/>
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            this.query = SystemAPI.QueryBuilder().WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build();
            state.RequireForUpdate(this.query);

            this.hasPresentation = state.WorldUnmanaged.SystemExists<BeginPresentationEntityCommandBufferSystem>();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            SystemAPI.GetSingleton<BLDebug>().Info("Game Paused: true");
            this.pauseTime = SystemAPI.Time.ElapsedTime;
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            SystemAPI.GetSingleton<BLDebug>().Info("Game Paused: false");
            this.Unpause(ref state);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var pauses = this.query.ToComponentDataArray<PauseGame>(state.WorldUpdateAllocator);
            var pausePresentation = false;
            foreach (var p in pauses)
            {
                if (p.PausePresentation)
                {
                    pausePresentation = true;
                    break;
                }
            }

            this.Pause(ref state, pausePresentation);

            // Game time progress needs to be paused to stop fixed step catchup after unpausing
            state.WorldUnmanaged.Time = new TimeData(this.pauseTime, state.WorldUnmanaged.Time.DeltaTime);
        }

        private void Pause(ref SystemState state, bool pausePresentation)
        {
            ref var simulationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<SimulationSystemGroup>();
            simulationSystemGroup.Enabled = false;

            // We only pause presentation for initial pause for subscene loading
            // In game pauses should continue to tick presentation
            if (this.hasPresentation)
            {
                ref var presentationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<PresentationSystemGroup>();
                presentationSystemGroup.Enabled = !pausePresentation;
            }
        }

        private void Unpause(ref SystemState state)
        {
            ref var simulationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<SimulationSystemGroup>();
            simulationSystemGroup.Enabled = true;

            if (this.hasPresentation)
            {
                ref var presentationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<PresentationSystemGroup>();
                presentationSystemGroup.Enabled = true;
            }
        }
    }
}
#endif
