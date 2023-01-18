// <copyright file="SettingsAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using System.Linq;
    using Unity.Entities;
    using UnityEngine;

    public class SettingsAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Settings.Settings[] settings;

        public Settings.Settings[] Settings => this.settings;
    }

    public class SettingsBaker : Baker<SettingsAuthoring>
    {
        public override void Bake(SettingsAuthoring authoring)
        {
            if (authoring.Settings == null)
            {
                return;
            }

            foreach (var setting in authoring.Settings.Distinct())
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
