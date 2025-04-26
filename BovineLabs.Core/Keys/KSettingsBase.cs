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
    [ResourceSettings(KResourceDirectory)]
    public abstract class KSettingsBase : ScriptableObject, ISettings
    {
        public const string KResourceDirectory = "K";

        [Multiline]
        [UsedImplicitly]
        [SerializeField]
        private string description = string.Empty;

        protected abstract void Initialize();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void LoadAll()
        {
            var kvSettings = Resources.LoadAll<KSettingsBase>(KResourceDirectory);

            foreach (var setting in kvSettings)
            {
                setting.Initialize();
            }
        }
    }
}
