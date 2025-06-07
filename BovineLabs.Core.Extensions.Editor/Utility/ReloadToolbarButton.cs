// <copyright file="ReloadToolbarButton.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Utility
{
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Internal;
    using BovineLabs.Core.Editor.UI;
    using Unity.Burst;
    using UnityEditor;
    using UnityEditor.Toolbars;
    using UnityEngine;
    using UnityEngine.UIElements;

    [Configurable]
    public static class ReloadToolbarButton
    {
        [ConfigVar("debug.reload-toolbar-button", true, "Should the reload toolbar button be shown. Requires a domain reload to update")]
        private static readonly SharedStatic<bool> Enabled = SharedStatic<bool>.GetOrCreate<EnabledType>();

        private static EditorToolbarDropdown? dropDown;

        [EditorToolbar(EditorToolbarPosition.RightCenter, -20)]
        public static VisualElement? RequestScriptReload()
        {
            ConfigVarManager.Init();

            if (!Enabled.Data)
            {
                return null;
            }

            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            dropDown = new EditorToolbarDropdown { icon = (Texture2D)EditorGUIUtility.IconContent("Refresh").image, tooltip = "Reload" };
            dropDown.AddToClassList("unity-editor-toolbar-element");
            dropDown.clicked += () => ClickEvent(dropDown.worldBound);
            return dropDown;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange obj)
        {
            switch (obj)
            {
                case PlayModeStateChange.EnteredPlayMode:
                    dropDown?.SetEnabled(false);
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    dropDown?.SetEnabled(true);
                    break;
            }
        }

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
