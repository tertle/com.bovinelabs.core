// <copyright file="SceneInitializeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.LifeCycle
{
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.Pause;
    using Unity.Entities;

    /// <summary>
    /// Initializes entities that were created from subscenes or ghosts (if using netcode).
    /// </summary>
    [UpdateInGroup(typeof(BeginSimulationSystemGroup), OrderFirst = true)]
    public partial class SceneInitializeSystem : SystemBase, IUpdateWhilePaused
    {
        private InitializeSystemGroup initializeSystemGroup;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.initializeSystemGroup = this.World.GetExistingSystemManaged<InitializeSystemGroup>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            this.initializeSystemGroup.Update();
        }
    }
}
#endif