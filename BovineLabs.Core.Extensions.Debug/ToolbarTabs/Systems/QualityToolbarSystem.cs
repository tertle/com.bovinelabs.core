// <copyright file="QualityToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine;

    [WorldSystemFilter(Worlds.Service)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial struct QualityToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<QualityToolbarBindings, QualityToolbarBindings.Data> toolbar;
        private int lastQuality;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<QualityToolbarBindings, QualityToolbarBindings.Data>(ref state, "Quality", "quality");
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
            ref var binding = ref this.toolbar.Binding;

            if (this.lastQuality != binding.Quality)
            {
                QualitySettings.SetQualityLevel(binding.Quality);
                this.lastQuality = binding.Quality;
            }
            else
            {
                binding.Quality = QualitySettings.GetQualityLevel();
                this.lastQuality = binding.Quality;
            }
        }
    }
}
#endif
