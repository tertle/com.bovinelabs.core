namespace BovineLabs.Core.Editor.EditorPreferences
{
    using System.Collections.Generic;
    using UnityEditor;

    internal sealed class CoreEditorPreferencesProvider : EditorPreferences<CoreEditorPreferenceAttribute>
    {
        public const string PreferencesPath = "BovineLabs";

        private CoreEditorPreferencesProvider(IEnumerable<string> keywords = null)
            : base(PreferencesPath, SettingsScope.User, keywords)
        {
        }

        [SettingsProvider]
        public static SettingsProvider GetPreferences()
        {
            return HasAnyPreferences
                ? new CoreEditorPreferencesProvider()
                : null;
        }
    }
}
