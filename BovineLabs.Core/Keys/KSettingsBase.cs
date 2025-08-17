// <copyright file="KSettingsBase.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using BovineLabs.Core.Settings;
    using JetBrains.Annotations;
    using UnityEngine;

    /// <summary>
    /// The base KSettings file for defining custom enums, layers, keys. Do not implement this directly, implement <see cref="KSettings{T,TV}" />.
    /// </summary>
    [Serializable]
    [SettingSubDirectory("K")]
    public abstract class KSettingsBase : SettingsSingleton
    {
        [Multiline]
        [UsedImplicitly]
        [SerializeField]
        private string description = string.Empty;
    }
}
