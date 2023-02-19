// <copyright file="SelectedEntityEditorSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using BovineLabs.Core.Editor.Internal;
    using Unity.Entities;

    /// <summary>
    /// This system does nothing except create the <see cref="SelectedEntity"/> in debug builds.
    /// In editor this is handled by SelectedEntityEditorSystem.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SelectedEntityEditorSystem : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.EntityManager.CreateEntity(typeof(SelectedEntity));
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var selectedEntity = default(SelectedEntity);

            if (EntitySelection.IsSelected && EntitySelection.World == this.World)
            {
                selectedEntity.Value = EntitySelection.Entity;
            }

            SystemAPI.SetSingleton(selectedEntity);
        }
    }
}
