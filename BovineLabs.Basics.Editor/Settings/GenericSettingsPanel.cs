// <copyright file="GenericSettingsPanel.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.Editor.Settings
{
    using BovineLabs.Basics.Settings;
    using UnityEngine;

    internal sealed class GenericSettingsPanel<T> : SettingsBasePanel<T>
        where T : ScriptableObject, ISettings
    {
    }
}