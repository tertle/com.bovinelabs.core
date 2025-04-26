// <copyright file="EditorSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using BovineLabs.Core.Authoring.Settings;
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(EditorSettings))]
    public class EditorSettingsEditor : ElementEditor
    {
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            var editorSettings = (EditorSettings)this.target;

            var button = new Button(() => UpdateSettings(editorSettings))
            {
                text = "Update Settings",
                style = { maxWidth = 200 },
            };

            root.Add(button);
        }

        private static void UpdateSettings(EditorSettings editorSettings)
        {
            if (editorSettings.DefaultSettingsAuthoring == null)
            {
                return;
            }

            // Clear all our existing settings
            ClearSettings(editorSettings.DefaultSettingsAuthoring);
            foreach (var i in editorSettings.SettingsAuthorings)
            {
                ClearSettings(i.Authoring);
            }

            foreach (var guid in AssetDatabase.FindAssets("t:SettingsBase"))
            {
                var settingsBase = AssetDatabase.LoadAssetAtPath<SettingsBase>(AssetDatabase.GUIDToAssetPath(guid));
                if (settingsBase == null)
                {
                    continue;
                }

                EditorSettingsUtility.AddSettingsToAuthoring(editorSettings, settingsBase);
            }

            return;

            static void ClearSettings(SettingsAuthoring? authoring)
            {
                if (authoring == null)
                {
                    return;
                }

                var so = new SerializedObject(authoring);
                var settingsProperty = so.FindProperty("settings");
                settingsProperty.arraySize = 0;
                so.ApplyModifiedProperties();
                AssetDatabase.SaveAssetIfDirty(authoring);
            }
        }
    }
}
