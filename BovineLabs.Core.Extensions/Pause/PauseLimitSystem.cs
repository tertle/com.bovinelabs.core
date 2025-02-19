// <copyright file="PauseLimitSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PAUSE
namespace BovineLabs.Core.Pause
{
    using Unity.Entities;

    [UpdateAfter(typeof(EndInitializationEntityCommandBufferSystem))]
    [UpdateInGroup(typeof(InitializationSystemGroup), OrderLast = true)]
    [WorldSystemFilter(Worlds.Service | Worlds.SimulationThin)]
    public partial class PauseLimitSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnUpdate()
        {
            var simulationSystemGroup = this.World.GetExistingSystemManaged<SimulationSystemGroup>();
            simulationSystemGroup.RateManager = new PauseRateManager(simulationSystemGroup);

            var presentationSystemGroup = this.World.GetExistingSystemManaged<PresentationSystemGroup>();
            if (presentationSystemGroup != null)
            {
                presentationSystemGroup.RateManager = new PauseRateManager(presentationSystemGroup);
            }

            this.World.GetExistingSystemManaged<InitializationSystemGroup>().RemoveSystemFromUpdateList(this);
        }
    }
}
#endif
