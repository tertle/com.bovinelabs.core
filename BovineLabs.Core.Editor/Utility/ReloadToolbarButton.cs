// <copyright file="ReloadToolbarButton.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Utility
{
    using BovineLabs.Core.Editor.Internal;
    using JetBrains.Annotations;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEditor.Scripting.LifecycleManagement;
    using UnityEditor.Toolbars;
    using UnityEngine;

    [UsedImplicitly]
    public static partial class ReloadToolbarButton
    {
        private const string ReloadPath = "BovineLabs/Reload";

        [NoAutoStaticsCleanup]
        private static MainToolbarDropdown dropDown;

        [MainToolbarElement(ReloadPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        [UsedImplicitly]
        private static MainToolbarElement Reload()
        {
            var icon = (Texture2D)EditorGUIUtility.IconContent("Refresh").image;
            var content = new MainToolbarContent(icon, "Reload");
            dropDown = new MainToolbarDropdown(content, ClickEvent) { enabled = !EditorApplication.isPlaying };
            return dropDown;
        }

        [OnEnteringPlayMode]
        [OnEnteringEditMode]
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
