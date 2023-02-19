// <copyright file="PauseSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.App
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
    public partial struct PauseSystem : ISystem, ISystemStartStop
    {
        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            var query = SystemAPI.QueryBuilder().WithAll<PauseGame>().WithOptions(EntityQueryOptions.IncludeSystems).Build();
            state.RequireForUpdate(query);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnStartRunning(ref SystemState state)
        {
            this.SetEnabled(ref state, true);
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnStopRunning(ref SystemState state)
        {
            this.SetEnabled(ref state, false);
        }

        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local", Justification = "SystemAPI")]
        private void SetEnabled(ref SystemState state, bool pause)
        {
            SystemAPI.GetSingleton<BLDebug>().Info($"Game Paused: {pause}");

            ref var simulationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<SimulationSystemGroup>();
            simulationSystemGroup.Enabled = !pause;

            ref var presentationSystemGroup = ref state.WorldUnmanaged.GetExistingSystemState<PresentationSystemGroup>();
            presentationSystemGroup.Enabled = !pause;
        }
    }
}
