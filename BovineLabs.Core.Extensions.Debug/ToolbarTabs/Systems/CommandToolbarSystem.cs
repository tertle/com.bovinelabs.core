// <copyright file="CommandToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.UIElements;

    [WorldSystemFilter(Worlds.Service)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial struct CommandToolbarSystem : ISystem, ISystemStartStop
    {
        private ToolbarHelper<CommandToolbarBindings, CommandToolbarBindings.Data> toolbar;
        private float lastTimeScale;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<CommandToolbarBindings, CommandToolbarBindings.Data>(ref state, "Command", "command");
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
        }
    }
}
#endif
