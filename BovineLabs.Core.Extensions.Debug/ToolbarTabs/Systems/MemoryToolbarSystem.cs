// <copyright file="MemoryToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.Time;
    using BovineLabs.Core.Toolbar;
    using Unity.Burst;
    using Unity.Entities;
    using UnityEngine.Profiling;

    /// <summary> The toolbar for monitoring memory. </summary>
    [WorldSystemFilter(Worlds.Service)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial struct MemoryToolbarSystem : ISystem, ISystemStartStop
    {
        private float timeToTriggerUpdatesPassed;
        private ToolbarHelper<MemoryToolbarBindings, MemoryToolbarBindings.Data> toolbar;

        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            this.toolbar = new ToolbarHelper<MemoryToolbarBindings, MemoryToolbarBindings.Data>(ref state, "Memory", "memory");
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
            this.timeToTriggerUpdatesPassed += UnityEngine.Time.unscaledDeltaTime;

            if (!this.toolbar.IsVisible())
            {
                return;
            }

            if (this.timeToTriggerUpdatesPassed < ToolbarManager.DefaultUpdateRate)
            {
                return;
            }

            this.timeToTriggerUpdatesPassed = 0;

            ref var data = ref this.toolbar.Binding;
            data.TotalAllocatedMemory = Profiler.GetTotalAllocatedMemoryLong();
            data.TotalReservedMemory = Profiler.GetTotalReservedMemoryLong();
            data.MonoUsedSize = Profiler.GetMonoUsedSizeLong();
            data.AllocatedMemoryForGraphics = Profiler.GetAllocatedMemoryForGraphicsDriver();
        }
    }
}
#endif
