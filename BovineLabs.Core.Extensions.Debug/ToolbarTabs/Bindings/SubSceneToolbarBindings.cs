// <copyright file="SubSceneToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.ToolbarTabs
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.UI;
    using JetBrains.Annotations;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Properties;
    using UnityEngine.UIElements;

    public class SubSceneToolbarBindings : IBindingObjectNotify<SubSceneToolbarBindings.Data>
    {
        private readonly List<string> subScenes = new();

        private Data value;

        public event EventHandler<BindablePropertyChangedEventArgs>? propertyChanged;

        public ref Data Value => ref this.value;

        [CreateProperty]
        [UsedImplicitly]
        public List<string> SubScenes
        {
            get
            {
                if (this.value.SubScenes.IsCreated)
                {
                    this.subScenes.Clear();
                    foreach (var n in this.value.SubScenes.AsArray())
                    {
                        this.subScenes.Add(n.ToString());
                    }
                }

                return this.subScenes;
            }
        }

        [CreateProperty]
        [UsedImplicitly]
        public BitArray256 SubSceneMask
        {
            get => this.value.SubSceneMask;
            set => this.value.SubSceneMask = value;
        }

        public void OnPropertyChanged(in FixedString64Bytes property)
        {
            this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property.ToString()));
        }

        public struct Data : IBindingObjectNotifyData
        {
            private BitArray256 subSceneMask;
            private NativeList<FixedString64Bytes> subScenes;

            public BitArray256 SubSceneMask
            {
                readonly get => this.subSceneMask;
                set
                {
                    if (this.subSceneMask != value)
                    {
                        this.subSceneMask = value;
                        this.Notify();
                    }
                }
            }

            public NativeList<FixedString64Bytes> SubScenes
            {
                readonly get => this.subScenes;
                set
                {
                    this.subScenes = value;
                    this.Notify();
                }
            }

            public FunctionPointer<OnPropertyChangedDelegate> Notify { get; set; }
        }
    }
}
#endif
