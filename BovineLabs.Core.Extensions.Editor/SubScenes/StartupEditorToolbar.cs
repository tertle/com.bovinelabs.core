// <copyright file="StartupEditorToolbar.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE && UNITY_6000_3_OR_NEWER
namespace BovineLabs.Core.Editor.SubScenes
{
    using System.Linq;
    using BovineLabs.Core.Authoring.SubScenes;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Extensions;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;

    public static class StartupEditorToolbar
    {
        private const string StartupPath = "BovineLabs/Startup";
        private const int MaxLength = 25;

        private static MainToolbarDropdown? startupDropDown;

        static StartupEditorToolbar()
        {
            EditorApplication.playModeStateChanged += change =>
            {
                switch (change)
                {
                    case PlayModeStateChange.EnteredPlayMode:
                    case PlayModeStateChange.EnteredEditMode:
                        RefreshStartup();
                        break;
                }
            };
        }

        [UsedImplicitly]
        [MainToolbarPreset]
        [MainToolbarElement(StartupPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement Startup()
        {
            var icon = (Texture2D)EditorGUIUtility.IconContent("PlayButton").image;
            var content = new MainToolbarContent(GetSelectionText(), icon, "Overrides the startup set of scenes for quick testing.");
            startupDropDown = new MainToolbarDropdown(content, EditorStartupDropDown) { enabled = !EditorApplication.isPlayingOrWillChangePlaymode };
            return startupDropDown;
        }

        private static string GetSelectionText()
        {
            const string defaultText = "Startup";

            var index = SubSceneEditorSystem.Override.Data;

            if (index < 0)
            {
                return defaultText;
            }

            var sets = EditorSettingsUtility.GetSettings<SubSceneSettings>().EditorSceneSets;
            if (index >= sets.Count || !sets[index])
            {
                SubSceneEditorSystem.Override.Data = -1;
                return defaultText;
            }

            return sets[index].name.ToSentence().Max(MaxLength, "...");
        }

        private static void EditorStartupDropDown(Rect worldBound)
        {
            var menu = new GenericMenu();

            var settings = EditorSettingsUtility.GetSettings<SubSceneSettings>();
            var sets = settings.EditorSceneSets.Select((set, index) => (set, index)).Where(s => s.set).OrderBy(s => s.set.name);

            foreach (var kvp in sets)
            {
                menu.AddItem(EditorGUIUtility.TrTextContent(kvp.set.name.ToSentence()), SubSceneEditorSystem.Override.Data == kvp.index, static data =>
                {
                    SubSceneEditorSystem.Override.Data = SubSceneEditorSystem.Override.Data == (int)data ? -1 : (int)data;
                    RefreshStartup();
                }, kvp.index);
            }

            menu.DropDown(worldBound);
        }

        private static void RefreshStartup()
        {
            MainToolbar.Refresh(StartupPath);
        }
    }
}
#endif
