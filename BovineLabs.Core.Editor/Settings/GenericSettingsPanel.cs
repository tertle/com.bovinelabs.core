// <copyright file="GenericSettingsPanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using BovineLabs.Core.Settings;
    using UnityEngine;

    internal sealed class GenericSettingsPanel<T> : SettingsBasePanel<T>
        where T : ScriptableObject, ISettings
    {
    }
}
