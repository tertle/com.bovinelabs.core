// <copyright file="EntityPickerTool.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_RENDERER
namespace BovineLabs.Core.Editor.ScenePicking
{
    using Unity.Entities;
    using UnityEditor;
    using UnityEditor.EditorTools;
    using UnityEngine;

    [EditorTool("Pick Entity")]
    public class EntityPickerTool : EditorTool
    {
        private EntityPickerSceneSystem system;
        private GUIContent toolbarContent;

        /// <inheritdoc/>
        public override GUIContent toolbarIcon
        {
            get
            {
                if (this.toolbarContent == null)
                {
                    return this.toolbarContent;
                }

                var iconPath = EditorGUIUtility.isProSkin
                    ? "Packages/com.unity.entities/Editor Default Resources/icons/dark/EntityGroup/EntityGroup.png"
                    : "Packages/com.unity.entities/Editor Default Resources/icons/light/EntityGroup/EntityGroup.png";

                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(iconPath);
                return this.toolbarContent = new GUIContent(icon, "Allows you to pick entities from the scene view");
            }
        }

        /// <inheritdoc/>
        public override void OnToolGUI(EditorWindow window)
        {
            if (window is not SceneView sceneView)
            {
                return;
            }

            if (this.system?.World == null || !this.system.World.IsCreated)
            {
                foreach (var w in World.All)
                {
                    this.system = w.GetExistingSystem<EntityPickerSceneSystem>();
                    if (this.system != null)
                    {
                        break;
                    }
                }
            }

            this.system?.OnScene(sceneView);

        }
    }
}
#endif
