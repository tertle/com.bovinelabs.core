// <copyright file="AlwaysUpdateSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SubScenes
{
    using BovineLabs.Core.Pause;
    using Unity.Entities;

    /// <summary> A <see cref="ComponentSystemGroup"/> that still updates while paused except if SubScenes haven't loaded. </summary>
    public abstract partial class AlwaysUpdateSystemGroup : ComponentSystemGroup, IUpdateWhilePaused // TODO rename
    {
#if !BL_DISABLE_SUBSCENE
        private SystemHandle subSceneLoadingSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.subSceneLoadingSystem = this.World.GetExistingSystem<SubSceneLoadingSystem>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            if (SystemAPI.HasComponent<PauseGame>(this.subSceneLoadingSystem))
            {
                return;
            }

            base.OnUpdate();
        }
#endif
    }
}
