// <copyright file="ReloadToolbarButton.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Utility
{
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Internal;
    using JetBrains.Annotations;
    using Unity.Burst;
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;
#if !UNITY_6000_3_OR_NEWER
    using BovineLabs.Core.Editor.UI;
    using UnityEngine.UIElements;
#endif

    [Configurable]
    [UsedImplicitly]
    public static class ReloadToolbarButton
    {
        private const string ReloadPath = "BovineLabs/Reload";

        [ConfigVar("debug.editor-toolbar.reload-button", true, "Should the reload toolbar button be shown. Requires a domain reload to update")]
        private static readonly SharedStatic<bool> Enabled = SharedStatic<bool>.GetOrCreate<EnabledType>();

#if UNITY_6000_3_OR_NEWER
        private static MainToolbarDropdown? dropDown;
#else
        private static EditorToolbarDropdown? dropDown;
#endif

#if UNITY_6000_3_OR_NEWER
        [MainToolbarElement(ReloadPath, defaultDockPosition = MainToolbarDockPosition.Middle)]
        public static MainToolbarElement? Reload()
#else
        [EditorToolbar(EditorToolbarPosition.RightCenter, -20)]
        public static VisualElement? Reload()
#endif
        {
            ConfigVarManager.Init();

            if (!Enabled.Data)
            {
                return null;
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

#if UNITY_6000_3_OR_NEWER
            var icon = (Texture2D)EditorGUIUtility.IconContent("Refresh").image;
            var content = new MainToolbarContent(icon, "Reload");
            dropDown = new MainToolbarDropdown(content, ClickEvent) { enabled = !EditorApplication.isPlayingOrWillChangePlaymode };
#else
            dropDown = new EditorToolbarDropdown { icon = (Texture2D)EditorGUIUtility.IconContent("Refresh").image, tooltip = "Reload" };
            dropDown.AddToClassList("unity-editor-toolbar-element");
            dropDown.clicked += () => ClickEvent(dropDown.worldBound);
#endif
            return dropDown;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
#if UNITY_6000_3_OR_NEWER
                case PlayModeStateChange.EnteredPlayMode:
                case PlayModeStateChange.ExitingPlayMode:
                    RefreshReload();
                    break;
#else
                case PlayModeStateChange.EnteredPlayMode:
                    dropDown?.SetEnabled(false);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    dropDown?.SetEnabled(true);
                    break;
#endif
            }
        }

#if UNITY_6000_3_OR_NEWER
        private static void RefreshReload()
        {
            MainToolbar.Refresh(ReloadPath);
        }
#endif

        private static void ClickEvent(Rect worldBound)
        {
            var menu = new GenericMenu();
            menu.AddItem(EditorGUIUtility.TrTextContent("Domain"), false, EditorUtility.RequestScriptReload);
            menu.AddItem(EditorGUIUtility.TrTextContent("SubScenes"), false, EntitiesCacheUtility.UpdateEntitySceneGlobalDependency);
            menu.DropDown(worldBound);
        }

        private struct EnabledType
        {
        }
    }
}