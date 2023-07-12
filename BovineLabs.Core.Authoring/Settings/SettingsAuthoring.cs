﻿// <copyright file="SettingsAuthoring.cs" company="BovineLabs">
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

        public SettingsBase[] Settings => this.settings;
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