// <copyright file="SettingsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Settings
{
    using System;
    using System.Linq;
    using Unity.Entities;
    using UnityEngine;

    public class SettingsAuthoring : MonoBehaviour
    {
        [SerializeField]
        private SettingsBase[] settings = Array.Empty<SettingsBase>();

        /// <inheritdoc />
        private class Baker : Baker<SettingsAuthoring>
        {
            /// <inheritdoc />
            public override void Bake(SettingsAuthoring authoring)
            {
                foreach (var setting in authoring.settings.Distinct())
                {
                    if (!setting)
                    {
                        BLGlobalLogger.LogWarning512($"Setting is not set on {authoring.gameObject.name} in {authoring.gameObject.scene.name}");
                        continue;
                    }

                    this.DependsOn(setting);
                    setting.Bake(this);
                }
            }
        }
    }
}
