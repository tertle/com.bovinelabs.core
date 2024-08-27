// <copyright file="UISettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.Authoring.UI
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using BovineLabs.Core.UI;
    using Unity.Entities;

    [SettingsGroup("Core")]
    [SettingsWorld("Shared")]
    public class UISettings : SettingsBase
    {
        /// <inheritdoc />
        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            var entity = baker.GetEntity(TransformUsageFlags.None);

            baker.AddComponent<UIState>(entity);
            baker.AddComponent<UIStatePrevious>(entity);
            baker.AddBuffer<UIStateBack>(entity);
            baker.AddBuffer<UIStateForward>(entity);
        }
    }
}
#endif
