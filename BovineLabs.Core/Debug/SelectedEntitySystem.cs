// <copyright file="SelectedEntitySystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_EDITOR && BL_DEBUG
namespace BovineLabs.Core
{
    using Unity.Entities;

    /// <summary>
    /// This system does nothing except create the <see cref="SelectedEntity"/> in debug builds.
    /// In editor this is handled by SelectedEntityEditorSystem.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SelectedEntitySystem : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.EntityManager.CreateEntity(typeof(SelectedEntity), typeof(SelectedEntities));
        }

        protected override void OnUpdate()
        {
            // In debug builds you can't select entities, at least not via this system
            this.World.GetExistingSystemManaged<InitializationSystemGroup>().RemoveSystemFromUpdateList(this);
        }
    }
}
#endif
