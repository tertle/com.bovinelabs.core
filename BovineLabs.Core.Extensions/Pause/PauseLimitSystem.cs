// <copyright file="PauseLimitSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PAUSE
namespace BovineLabs.Core.Pause
{
    using BovineLabs.Core.Extensions;
    using Unity.Entities;
#if UNITY_NETCODE
    using Unity.NetCode;
#endif

    [UpdateAfter(typeof(EndInitializationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [WorldSystemFilter(Worlds.Service | Worlds.Simulation)]
    public partial class PauseLimitSystem : SystemBase
    {
#if UNITY_NETCODE
        private bool updateNetworkTime;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.updateNetworkTime = this.World.Unmanaged.SystemExists<UpdateNetworkTimeSystem>();

            if (this.updateNetworkTime)
            {
                this.World.Unmanaged.GetExistingSystemState<UpdateNetworkTimeSystem>().Enabled = false;
            }
        }
#endif

        /// <inheritdoc/>
        protected override void OnStartRunning()
        {
            var simulationSystemGroup = this.World.GetExistingSystemManaged<SimulationSystemGroup>();
            simulationSystemGroup.RateManager = new PauseRateManager(simulationSystemGroup);

            var presentationSystemGroup = this.World.GetExistingSystemManaged<PresentationSystemGroup>();
            if (presentationSystemGroup != null)
            {
                presentationSystemGroup.RateManager = new PauseRateManager(presentationSystemGroup);
            }
        }

        /// <inheritdoc/>
        protected override void OnStopRunning()
        {
            var simulationSystemGroup = this.World.GetExistingSystemManaged<SimulationSystemGroup>();

            if (simulationSystemGroup.RateManager is PauseRateManager pauseRateManager)
            {
                simulationSystemGroup.RateManager = pauseRateManager.ExistingRateManager;
            }

            var presentationSystemGroup = this.World.GetExistingSystemManaged<PresentationSystemGroup>();
            if (presentationSystemGroup?.RateManager is PauseRateManager presentationRateManager)
            {
                presentationSystemGroup.RateManager = presentationRateManager.ExistingRateManager;
            }
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
#if UNITY_NETCODE
            // This replicates the behaviour of UpdateNetworkTimeSystem in netcode
            if (this.updateNetworkTime)
            {
                // need to set IsOffFrame outside of the SimulationGroup rate managers, so it can be accessed by user systems outside of that group. That's because
                // server worlds just don't run the simulation group, so users would never be able to read a valid value.
                ref var networkTime = ref SystemAPI.GetSingletonRW<NetworkTime>().ValueRW;
                var rateManager = this.World.GetExistingSystemManaged<SimulationSystemGroup>().RateManager;

                var pauseRateManager = (PauseRateManager)rateManager;

                if (this.World.IsServer())
                {
                    if (this.World.IsHost())
                    {
                        var hostRateManager = (NetcodeHostRateManager)pauseRateManager.ExistingRateManager;
                        networkTime.IsOffFrame = !hostRateManager.WillUpdateInternal();
                    }
                    else
                    {
                        var serverRateManager = (NetcodeServerRateManager)pauseRateManager.ExistingRateManager;
                        networkTime.IsOffFrame = !serverRateManager.WillUpdateInternal();
                    }
                }
                else
                {
                    networkTime.IsOffFrame = false; // clients have partial ticks, they always tick
                }
            }
            else
#endif
            {
                this.World.GetExistingSystemManaged<InitializationSystemGroup>().RemoveSystemFromUpdateList(this);
            }
        }
    }
}
#endif
