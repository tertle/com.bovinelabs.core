// <copyright file="InputSettingsBaker.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input.Authoring
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using Unity.Entities;

    [SettingsGroup("Core")]
    [SettingsWorld("Client")]
    public class InputSettingsBaker : SettingsBase
    {
        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var wrapper = new BakerWrapper(baker, baker.GetEntity(TransformUsageFlags.None));

            foreach (var s in InputCommonSettings.I.Settings)
            {
                s?.Bake(wrapper);
            }

#if !BL_DEBUG
            if (baker.IsBakingForEditor())
#endif
            {
                foreach (var s in InputCommonSettings.I.DebugSettings)
                {
                    s?.Bake(wrapper);
                }
            }
        }

        private class BakerWrapper : IBakerWrapper
        {
            private readonly IBaker baker;
            private readonly Entity entity;

            public BakerWrapper(IBaker baker, Entity entity)
            {
                this.baker = baker;
                this.entity = entity;
            }

            public void AddComponent<T>(T component)
                where T : unmanaged, IComponentData
            {
                this.baker.AddComponent(this.entity, component);
            }
        }
    }
}
