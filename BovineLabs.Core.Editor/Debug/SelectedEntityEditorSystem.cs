// <copyright file="SelectedEntityEditorSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor
{
    using BovineLabs.Core.Editor.Internal;
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SelectedEntityEditorSystem : SystemBase
    {
        /// <inheritdoc/>
        protected override void OnCreate()
        {
            this.EntityManager.CreateEntity(typeof(SelectedEntity), typeof(SelectedEntities));
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var selectedEntity = default(SelectedEntity);

            var selectedEntities = SystemAPI.GetSingletonBuffer<SelectedEntities>();
            selectedEntities.Clear();

            foreach (var entity in EntitySelection.GetAllSelectionsInWorld(this.World))
            {
                if (selectedEntity.Value == Entity.Null)
                {
                    selectedEntity.Value = entity;
                }

                selectedEntities.Add(new SelectedEntities { Value = entity });
            }

            SystemAPI.SetSingleton(selectedEntity);
        }
    }
}
