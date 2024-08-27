// <copyright file="GameSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Authoring
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Settings;
    using BovineLabs.Core.SubScenes;
    using Unity.Entities;
    using Unity.Mathematics;
    using UnityEngine;

    [SettingsGroup("Core")]
    [SettingsWorld("Shared")]
    public class GameSettings : Settings.SettingsBase
    {
        [Min(0)]
        [SerializeField]
        private float loadMaxDistance = 128;

        [Min(0)]
        [SerializeField]
        private float unloadMaxDistance = 144;

        /// <inheritdoc />
        public override void Bake(Baker<SettingsAuthoring> baker)
        {
            baker.AddComponent(
                baker.GetEntity(TransformUsageFlags.None),
                new LoadWithBoundingVolumeConfig
                {
                    LoadMaxDistance = this.loadMaxDistance,
                    UnloadMaxDistance = this.unloadMaxDistance,
                });
        }

        private void OnValidate()
        {
            this.unloadMaxDistance = math.max(this.unloadMaxDistance, this.loadMaxDistance);
        }
    }
}
#endif
