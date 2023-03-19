// <copyright file="SettingsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using System;
    using System.Linq;
    using Unity.Entities;
    using UnityEngine;

    public class SettingsAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Settings.Settings[] settings = Array.Empty<Settings.Settings>();

        public Settings.Settings[] Settings => this.settings;
    }

    public class SettingsBaker : Baker<SettingsAuthoring>
    {
        public override void Bake(SettingsAuthoring authoring)
        {
            foreach (var setting in authoring.Settings.Distinct())
            {
                if (setting == null)
                {
                    Debug.LogWarning("Setting is not set");
                    continue;
                }

                this.DependsOn(setting);
                setting.Bake(this);
            }
        }
    }
}
