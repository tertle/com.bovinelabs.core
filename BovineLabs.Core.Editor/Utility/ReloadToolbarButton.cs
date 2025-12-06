// <copyright file="ReloadToolbarButton.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_6000_3_OR_NEWER
namespace BovineLabs.Core.Editor.Utility
{
    using BovineLabs.Core.Editor.Internal;
    using JetBrains.Annotations;
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;

    [UsedImplicitly]
    public static class ReloadToolbarButton
    {
        private const string ReloadPath = "BovineLabs/Reload";
        private static MainToolbarDropdown? dropDown;

        static ReloadToolbarButton()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MainToolbarPreset]
        [MainToolbarElement(ReloadPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        [UsedImplicitly]
        private static MainToolbarElement Reload()
        {
            var icon = (Texture2D)EditorGUIUtility.IconContent("Refresh").image;
            var content = new MainToolbarContent(icon, "Reload");
            dropDown = new MainToolbarDropdown(content, ClickEvent) { enabled = !EditorApplication.isPlayingOrWillChangePlaymode };
            return dropDown;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.ExitingPlayMode:
                    RefreshReload();
                    break;
            }
        }

        private static void RefreshReload()
        {
            MainToolbar.Refresh(ReloadPath);
        }

        private static void ClickEvent(Rect worldBound)
        {
            var menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Domain"), false, EditorUtility.RequestScriptReload);
            menu.AddItem(EditorGUIUtility.TrTextContent("SubScenes"), false, EntitiesCacheUtility.UpdateEntitySceneGlobalDependency);
            menu.DropDown(worldBound);
        }
    }
}
#endif