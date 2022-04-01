// <copyright file="SettingsEditorToolbar.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System.Linq;
    using BovineLabs.Core.Editor.EditorToolbar;
    using Unity.Scenes;
    using Unity.Scenes.Editor;
    using UnityEditor;
    using UnityEngine;

    [InitializeOnLoad]
    public static class SettingsEditorToolbar
    {
        private static readonly GUIContent SceneContent;
        private static GUIStyle dropDownStyle;

        private static bool initialized;

        static SettingsEditorToolbar()
        {
            ToolbarExtender.RightToolbarGUI.Add(OnToolbarGUI);

            SceneContent = EditorGUIUtility.TrTextContent("Settings", "Allows you to easily reimport sub scenes");
        }

        private static void Initialize()
        {
            // Styles need to be initialized inside OnGUI so the style sheet has been loaded by Unity
            dropDownStyle = "Dropdown";
        }

        private static void OnToolbarGUI()
        {
            if (!initialized)
            {
                initialized = true;
                Initialize();
            }

            var rect = GUILayoutUtility.GetRect(SceneContent, dropDownStyle, GUILayout.Width(ToolbarExtender.DropdownWidth));

            if (EditorGUI.DropdownButton(rect, SceneContent, FocusType.Passive, dropDownStyle))
            {
                ShowSubSceneMenu(rect);
            }
        }

        private static void ShowSubSceneMenu(Rect dropDownRect)
        {
            var genericMenu = new GenericMenu();

            genericMenu.AddItem(EditorGUIUtility.TrTextContent("Window"), false, () =>
            {
                SettingsBaseWindow<SettingsWindow>.Open();
            });

            var editorSettings = EditorSettingsUtility.GetSettings<EditorSettingsBase>();

            if (editorSettings.CorePrefab != null)
            {
                genericMenu.AddItem(EditorGUIUtility.TrTextContent("Apply"), false, () =>
                {
                    AssetDatabase.SaveAssets(); // TODO limit?

                    var subScenes = editorSettings.CorePrefab
                        .GetComponentsInChildren<SubScene>()
                        .Where(s => editorSettings.SettingSubScenes.Contains(s.SceneAsset))
                        .ToArray();

                    SubSceneInspectorUtility.ForceReimport(subScenes);
                });
            }

            genericMenu.DropDown(dropDownRect);
        }
    }
}
