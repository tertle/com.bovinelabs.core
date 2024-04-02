// <copyright file="TimeToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using System;
    using BovineLabs.Core.UI;
    using BovineLabs.Core.Utility;
    using JetBrains.Annotations;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Properties;
    using UnityEngine.UIElements;

    public class TimeToolbarBindings : IBindingObjectNotify<TimeToolbarBindings.Data>
    {
        private Data data;

        public event EventHandler<BindablePropertyChangedEventArgs>? propertyChanged;

        public ref Data Value => ref this.data;

        [CreateProperty]
        [UsedImplicitly]
        public float TimeScale
        {
            get => this.data.TimeScale;
            set => this.data.TimeScale = value;
        }

        [CreateProperty]
        [UsedImplicitly]
        public bool IsPaused
        {
            get => this.data.Paused;
            set => this.data.Paused = value;
        }

        public void OnPropertyChanged(in FixedString64Bytes property)
        {
            this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property.ToString()));
        }

        public struct Data : IBindingObjectNotifyData
        {
            private float timeScale;
            private bool paused;

            public float TimeScale
            {
                get => this.timeScale;
                set
                {
                    if (!mathex.Approximately(this.timeScale, value, 0.01f))
                    {
                        this.timeScale = math.max(0, value);
                        this.Notify();
                    }
                }
            }

            public bool Paused
            {
                readonly get => this.paused;
                set
                {
                    if (this.paused != value)
                    {
                        this.paused = value;
                        this.Notify();
                    }
                }
            }

            public FunctionPointer<OnPropertyChangedDelegate> Notify { get; set; }
        }
    }
}
#endif
