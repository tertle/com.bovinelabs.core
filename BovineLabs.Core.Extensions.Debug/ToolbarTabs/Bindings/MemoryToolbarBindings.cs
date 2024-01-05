// <copyright file="MemoryToolbarBindings.cs" company="BovineLabs">
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

    public class MemoryToolbarBindings : IBindingObjectNotify<MemoryToolbarBindings.Data>
    {
        private MemoryToolbarBindings.Data data;

        public event EventHandler<BindablePropertyChangedEventArgs>? propertyChanged;

        public ref MemoryToolbarBindings.Data Value => ref this.data;

        [CreateProperty]
        public long TotalAllocatedMemory => this.data.TotalAllocatedMemory;

        [CreateProperty]
        public long TotalReservedMemory => this.data.TotalReservedMemory;

        [CreateProperty]
        public long MonoUsedSize => this.data.MonoUsedSize;

        [CreateProperty]
        public long AllocatedMemoryForGraphics => this.data.AllocatedMemoryForGraphics;

        [Initialize]
        public static void RegisterConverters()
        {
            const float megaByte = 1024 * 1024;
            var groupFPS = new ConverterGroup("Bytes to MegaBytes");
            groupFPS.AddConverter((ref long value) => $"{value / megaByte:0.0} MB");
            ConverterGroups.RegisterConverterGroup(groupFPS);
        }

        public void OnPropertyChanged(in FixedString64Bytes property)
        {
            this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(property.ToString()));
        }

        public struct Data : IBindingObjectNotifyData
        {
            private long totalAllocatedMemory;
            private long totalReservedMemory;
            private long monoUsedSize;
            private long allocatedMemoryForGraphics;

            public long TotalAllocatedMemory
            {
                readonly get => this.totalAllocatedMemory;
                set
                {
                    if (this.totalAllocatedMemory == value) return;
                    this.totalAllocatedMemory = value;
                    this.Notify();
                }
            }

            public long TotalReservedMemory
            {
                readonly get => this.totalReservedMemory;
                set
                {
                    if (this.totalReservedMemory == value) return;
                    this.totalReservedMemory = value;
                    this.Notify();
                }
            }

            public long MonoUsedSize
            {
                readonly get => this.monoUsedSize;
                set
                {
                    if (this.monoUsedSize == value) return;
                    this.monoUsedSize = value;
                    this.Notify();
                }
            }

            public long AllocatedMemoryForGraphics
            {
                readonly get => this.allocatedMemoryForGraphics;
                set
                {
                    if (this.allocatedMemoryForGraphics == value) return;
                    this.allocatedMemoryForGraphics = value;
                    this.Notify();
                }
            }

            public FunctionPointer<OnPropertyChangedDelegate> Notify { get; set; }
        }
    }
}
#endif
