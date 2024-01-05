// <copyright file="FPSToolbarBindings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR
namespace BovineLabs.Core.ToolbarTabs
{
    using BovineLabs.Core.UI;
    using BovineLabs.Core.Utility;
    using Unity.Properties;
    using UnityEngine.UIElements;

    public class FPSToolbarBindings : IBindingObjectHash<FPSToolbarBindings.Data>
    {
        private Data data;

        public ref FPSToolbarBindings.Data Value => ref this.data;

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

        public struct Data : IBindingObjectHashData
        {
            public float CurrentFPS;
            public float AverageFPS;
            public float MinFPS;
            public float MaxFPS;

            public long Version { get; set; }
        }
    }
}
#endif
