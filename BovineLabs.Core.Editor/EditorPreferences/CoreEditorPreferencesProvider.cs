// <copyright file="CoreEditorPreferencesProvider.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.EditorPreferences
{
    using System.Collections.Generic;
    using UnityEditor;

    /// <summary>
    /// Settings provider for BovineLabs Core editor preferences.
    /// </summary>
    internal sealed class CoreEditorPreferencesProvider : EditorPreferences<CoreEditorPreferenceAttribute>
    {
        public const string PreferencesPath = "BovineLabs";

        /// <summary>
        /// Initializes a new instance of the <see cref="CoreEditorPreferencesProvider"/> class.
        /// </summary>
        /// <param name="keywords">Additional keywords for searching.</param>
        private CoreEditorPreferencesProvider(IEnumerable<string>? keywords = null)
            : base(PreferencesPath, SettingsScope.User, keywords)
        {
        }

        /// <summary>
        /// Creates the preferences provider for Core editor preferences.
        /// </summary>
        /// <returns>The settings provider instance, or null if no preferences are available.</returns>
        [SettingsProvider]
        public static SettingsProvider? GetPreferences()
        {
            return HasAnyPreferences
                ? new CoreEditorPreferencesProvider()
                : null;
        }
    }
}
