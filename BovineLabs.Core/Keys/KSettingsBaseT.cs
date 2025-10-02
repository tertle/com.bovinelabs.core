// <copyright file="KSettingsBaseT.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Settings;
    using JetBrains.Annotations;
    using UnityEngine;

    public interface IKsettings
    {
    }

    /// <summary>
    /// The base KSettings file for defining custom enums, layers, keys. Do not implement this directly, implement <see cref="KSettings{T,TV}" />.
    /// Instead implement <see cref="KSettings{T,TV}" /> or rarely <see cref="KSettingsBase{T,TV}" />.
    /// </summary>
    /// <typeparam name="TV"> The value. </typeparam>
    [Serializable]
    [SettingSubDirectory("K")]
    public abstract class KSettingsBase<TV> : SettingsSingleton, IKsettings
    {
        [Multiline]
        [UsedImplicitly]
        [SerializeField]
        private string description = string.Empty;

        public abstract IEnumerable<NameValue<TV>> Keys { get; }
    }
}
