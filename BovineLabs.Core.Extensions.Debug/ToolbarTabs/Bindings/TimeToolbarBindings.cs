// <copyright file="TimeToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.UI;
    using JetBrains.Annotations;
    using Unity.Mathematics;
    using Unity.Properties;
    using UnityEngine;

    public class TimeToolbarBindings : IBindingObject<TimeToolbarBindings.Data>
    {
        private TimeToolbarBindings.Data data;

        public ref TimeToolbarBindings.Data Value => ref this.data;

        [CreateProperty]
        [UsedImplicitly]
        public float TimeScale
        {
            get => Time.timeScale;
            set => Time.timeScale = math.max(0, value);
        }

        [CreateProperty]
        [UsedImplicitly]
        public bool IsPaused
        {
            get => this.data.Paused;
            set => this.data.Paused = value;
        }

        public struct Data
        {
            public bool Paused;
        }
    }
}
#endif
