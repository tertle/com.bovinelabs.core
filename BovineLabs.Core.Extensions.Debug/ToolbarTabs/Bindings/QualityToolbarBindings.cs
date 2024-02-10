// <copyright file="QualityToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.UI;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Properties;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class QualityToolbarBindings : IBindingObjectNotify<QualityToolbarBindings.Data>
    {
        private Data data;

        private List<string>? choices;

        public event EventHandler<BindablePropertyChangedEventArgs>? propertyChanged;

        public ref Data Value => ref this.data;

        [CreateProperty]
        public int QualityValue
        {
            get => this.data.Quality;
            set => this.data.Quality = value;
        }

        [CreateProperty]
        public List<string> QualityChoices => this.choices ??= QualitySettings.names.ToList();

        public void OnPropertyChanged(in FixedString64Bytes property)
        {
            this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property.ToString()));
        }

        public struct Data : IBindingObjectNotifyData
        {
            private int quality;

            public int Quality
            {
                readonly get => this.quality;
                set
                {
                    if (this.quality != value)
                    {
                        this.quality = value;
                        this.Notify();
                    }
                }

            }

            public FunctionPointer<OnPropertyChangedDelegate> Notify { get; set; }
        }

    }
}
#endif
