// <copyright file="EntitiesToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Entities;

    /// <summary> The toolbar for monitoring the number of entities, chunks and archetypes of a world. </summary>
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial struct EntitiesToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<EntitiesToolbarBindings, EntitiesToolbarBindings.Data> toolbar;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<EntitiesToolbarBindings, EntitiesToolbarBindings.Data>(ref state, "Entities", "entities");
        }

        /// <inheritdoc/>
        public void OnStartRunning(ref SystemState state)
        {
            this.toolbar.Load();
        }

        /// <inheritdoc/>
        public void OnStopRunning(ref SystemState state)
        {
            this.toolbar.Unload();
        }

        /// <inheritdoc/>
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            if (!this.toolbar.IsVisible())
            {
                return;
            }

            ref var data = ref this.toolbar.Binding;
            data.Entities = state.EntityManager.UniversalQuery.CalculateEntityCountWithoutFiltering();
            data.Archetypes = state.EntityManager.NumberOfArchetype();
            data.Chunks = state.EntityManager.UniversalQuery.CalculateChunkCountWithoutFiltering();
        }
    }
}
#endif
