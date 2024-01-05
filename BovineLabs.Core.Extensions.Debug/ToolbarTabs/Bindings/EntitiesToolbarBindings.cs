// <copyright file="EntitiesToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using System;
    using BovineLabs.Core.UI;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Properties;
    using UnityEngine.UIElements;

    public class EntitiesToolbarBindings : IBindingObjectNotify<EntitiesToolbarBindings.Data>
    {
        private Data data;

        public event EventHandler<BindablePropertyChangedEventArgs>? propertyChanged;

        public ref Data Value => ref this.data;

        [CreateProperty]
        public int Entities => this.data.Entities;

        [CreateProperty]
        public int Archetypes => this.data.Archetypes;

        [CreateProperty]
        public int Chunks => this.data.Chunks;

        public void OnPropertyChanged(in FixedString64Bytes property)
        {
            this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property.ToString()));
        }

        public struct Data : IBindingObjectNotifyData
        {
            private int entities;
            private int archetypes;
            private int chunks;

            public int Entities
            {
                readonly get => this.entities;
                set
                {
                    if (this.entities == value)
                    {
                        return;
                    }

                    this.entities = value;
                    this.Notify();
                }
            }

            public int Archetypes
            {
                readonly get => this.archetypes;
                set
                {
                    if (this.archetypes == value)
                    {
                        return;
                    }

                    this.archetypes = value;
                    this.Notify();
                }
            }

            public int Chunks
            {
                readonly get => this.chunks;
                set
                {
                    if (this.chunks == value)
                    {
                        return;
                    }

                    this.chunks = value;
                    this.Notify();
                }
            }

            public FunctionPointer<OnPropertyChangedDelegate> Notify { get; set; }
        }
    }
}
#endif
