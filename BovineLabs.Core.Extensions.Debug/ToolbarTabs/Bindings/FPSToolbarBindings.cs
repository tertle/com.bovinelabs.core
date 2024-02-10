// <copyright file="FPSToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using System;
    using BovineLabs.Core.UI;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Properties;
    using UnityEngine.UIElements;

    public class FPSToolbarBindings : IBindingObjectNotify<FPSToolbarBindings.Data>
    {
        private Data data;

        public event EventHandler<BindablePropertyChangedEventArgs>? propertyChanged;

        public ref Data Value => ref this.data;

        [CreateProperty]
        public float CurrentFPS => this.data.CurrentFPS;

        [CreateProperty]
        public float FrameTime => this.CurrentFPS == 0 ? 0 : 1000 / this.CurrentFPS;

        [CreateProperty]
        public float AverageFPS => this.data.AverageFPS;

        [CreateProperty]
        public float MinFPS => this.data.MinFPS;

        [CreateProperty]
        public float MaxFPS => this.data.MaxFPS;

        [Initialize]
        public static void RegisterConverters()
        {
            var groupFPS = new ConverterGroup("Value to fps");
            groupFPS.AddConverter((ref float value) => $"{(int)value} fps");
            ConverterGroups.RegisterConverterGroup(groupFPS);

            var groupTime = new ConverterGroup("Value to ms");
            groupTime.AddConverter((ref float value) => $"{value:0.0} ms");
            ConverterGroups.RegisterConverterGroup(groupTime);
        }

        public void OnPropertyChanged(in FixedString64Bytes property)
        {
            this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property.ToString()));
        }

        public struct Data : IBindingObjectNotifyData
        {
            private float currentFPS;
            private float averageFPS;
            private float minFPS;
            private float maxFPS;

            public float CurrentFPS
            {
                readonly get => this.currentFPS;
                set
                {
                    if (!mathex.Approximately(this.currentFPS, value, 0.0001f))
                    {
                        this.currentFPS = value;
                        this.Notify();
                    }
                }
            }

            public float AverageFPS
            {
                readonly get => this.averageFPS;
                set
                {
                    if (!mathex.Approximately(this.averageFPS, value, 0.01f))
                    {
                        this.averageFPS = value;
                        this.Notify();
                    }
                }
            }

            public float MinFPS
            {
                readonly get => this.minFPS;
                set
                {
                    if (!mathex.Approximately(this.minFPS, value, 0.01f))
                    {
                        this.minFPS = value;
                        this.Notify();
                    }
                }
            }

            public float MaxFPS
            {
                readonly get => this.maxFPS;
                set
                {
                    if (!mathex.Approximately(this.maxFPS, value, 0.01f))
                    {
                        this.maxFPS = value;
                        this.Notify();
                    }
                }
            }

            public FunctionPointer<OnPropertyChangedDelegate> Notify { get; set; }
        }
    }
}
#endif
