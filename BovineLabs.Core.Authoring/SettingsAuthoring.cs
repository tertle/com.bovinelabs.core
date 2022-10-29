// <copyright file="SettingsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using BovineLabs.Core.Settings;
    using Unity.Entities;
    using UnityEngine;

    public class SettingsAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Settings[] settings;

        public Settings[] Settings => this.settings;
    }

    public class SettingsBaker : Baker<SettingsAuthoring>
    {
        public override void Bake(SettingsAuthoring authoring)
        {
            if (authoring.Settings == null)
            {
                return;
            }

            foreach (var setting in authoring.Settings)
            {
                if (setting == null)
                {
                    Debug.LogWarning("Setting is null");
                    continue;
                }


                this.DependsOn(setting);
                setting.Bake(this);
            }
        }
    }
}
