// <copyright file="InitializationRequireSubScenesSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.SubScenes
{
    using BovineLabs.Core.Pause;
    using Unity.Entities;

#if !BL_DISABLE_SUBSCENE
    [CreateAfter(typeof(SubSceneLoadingSystem))]
#endif
    public abstract partial class InitializationRequireSubScenesSystemGroup : ComponentSystemGroup
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
