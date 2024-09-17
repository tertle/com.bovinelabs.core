// <copyright file="PauseLimitSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_PAUSE
namespace BovineLabs.Core.Pause
{
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(Worlds.Service | WorldSystemFilterFlags.LocalSimulation)]
    public partial class PauseLimitSystem : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnCreate()
        {
            var simulationSystemGroup = this.World.GetExistingSystemManaged<SimulationSystemGroup>();
            simulationSystemGroup.RateManager = new PauseRateManager(simulationSystemGroup, false);

            var presentationSystemGroup = this.World.GetExistingSystemManaged<PresentationSystemGroup>();
            if (presentationSystemGroup != null)
            {
                presentationSystemGroup.RateManager = new PauseRateManager(presentationSystemGroup, true);
            }

            this.Enabled = false;
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            // NO-OP
        }
    }
}
#endif
