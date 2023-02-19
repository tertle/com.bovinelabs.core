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
    public partial struct SelectedEntitySystem : ISystem
    {
        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.CreateEntity(typeof(SelectedEntity));

            // In debug builds you can't select entities, at least not via this system
            state.Enabled = false;
        }
    }
}
#endif
