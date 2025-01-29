// <copyright file="AssemblyGraphSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Dependency
{
    using BovineLabs.Core.Settings;
    using UnityEngine;

    [SettingsGroup("Core")]
    public class AssemblyGraphSettings : ScriptableObject, ISettings
    {
        [Tooltip("Uses EndsWith")]
        public string[] AssembliesToCheck = { ".Data", ".Authoring" };

        [Tooltip("Uses StartsWith")]
        public string[] AssembliesToIgnore = { "Unity", "BovineLabs.Core" };
    }
}
