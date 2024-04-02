// <copyright file="InitializationRequireSubScenesSystemGroup.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.App
{
    using Unity.Entities;

#if !BL_DISABLE_SUBSCENE
    [CreateAfter(typeof(SubScenes.SubSceneLoadingSystem))]
#endif
    public abstract partial class InitializationRequireSubScenesSystemGroup : ComponentSystemGroup
    {
#if !BL_DISABLE_SUBSCENE
        private SystemHandle subSceneLoadingSystem;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.subSceneLoadingSystem = this.World.GetExistingSystem<SubScenes.SubSceneLoadingSystem>();
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
#endif
