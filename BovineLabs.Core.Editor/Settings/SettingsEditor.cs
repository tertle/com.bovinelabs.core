// <copyright file="SettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Settings;
    using UnityEditor;

    [CustomEditor(typeof(Settings), true, isFallback = true)]
    public class SettingsEditor : UIElementsEditor
    {
    }
}
