// <copyright file="MemoryToolbarSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.Debug.ToolbarTabs
{
    using BovineLabs.Core.Debug.Toolbar;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.Profiling;
    using UnityEngine.UIElements;

    /// <summary> The toolbar for monitoring memory. </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Presentation)]
    [UpdateInGroup(typeof(ToolbarSystemGroup))]
    internal partial class MemoryToolbarSystem : ToolbarSystemBase
    {
        private const float MegaByte = 1024 * 1024;

        private Label? allocatedLabel;

        private VisualTreeAsset? asset;
        private Label? graphicsLabel;
        private Label? monoLabel;
        private Label? reservedLabel;

        private float timeToTriggerUpdatesPassed;

        /// <inheritdoc />
        protected override VisualTreeAsset Asset => this.asset!;

        /// <inheritdoc />
        protected override string Tab => "Monitor";

        /// <inheritdoc />
        protected override string Name => "Memory";

        /// <inheritdoc />
        protected override void OnCreateSystem()
        {
            this.asset = Resources.Load<VisualTreeAsset>("MemoryGroup");
        }

        /// <inheritdoc />
        protected override void OnLoad(VisualElement element)
        {
            this.allocatedLabel = element.Q<Label>("allocated");
            this.reservedLabel = element.Q<Label>("reserved");
            this.monoLabel = element.Q<Label>("mono");
            this.graphicsLabel = element.Q<Label>("graphics");
        }

        /// <inheritdoc />
        protected override void OnUpdateAlways()
        {
            this.timeToTriggerUpdatesPassed += UnityEngine.Time.unscaledDeltaTime;
        }

        /// <inheritdoc />
        protected override void OnUpdateVisible()
        {
            if (this.timeToTriggerUpdatesPassed < DefaultUpdateRate)
            {
                return;
            }

            this.timeToTriggerUpdatesPassed = 0;

            this.allocatedLabel!.text = $"{Profiler.GetTotalAllocatedMemoryLong() / MegaByte:0.0} MB";
            this.reservedLabel!.text = $"{Profiler.GetTotalReservedMemoryLong() / MegaByte:0.0} MB";
            this.monoLabel!.text = $"{Profiler.GetMonoUsedSizeLong() / MegaByte:0.0} MB";
            this.graphicsLabel!.text = $"{Profiler.GetAllocatedMemoryForGraphicsDriver() / MegaByte:0.0} MB";
        }
    }
}
#endif
