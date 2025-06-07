// <copyright file="VFXGraphTemplateWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_VFX_GRAPH
namespace BovineLabs.Core.Editor.VFXGraphTemplateWindow
{
    using BovineLabs.Core.Editor.UI;
    using UnityEditor;
    using UnityEditor.VFX;
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEngine.VFX;

    public class VFXGraphTemplateWindow : EditorWindow
    {
        private const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/VFXGraphTemplateWindow/";
        private static readonly UITemplate Window = new(RootUIPath + "VFXGraphTemplateWindow");

        private TextField? nameField;
        private TextField? categoryField;
        private TextField? descriptionField;

        [MenuItem(EditorMenus.RootMenuTools + "Create VFX Template", priority = -15)]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<VFXGraphTemplateWindow>();
            window.titleContent = new GUIContent("BovineLabs");
            window.Show();
        }

        private void OnEnable()
        {
            var root = this.rootVisualElement;

            Window.Clone(root);

            this.rootVisualElement.Q<Button>().clicked += this.CreateTemplate;
            this.nameField = this.rootVisualElement.Q<TextField>("Name");
            this.categoryField = this.rootVisualElement.Q<TextField>("Category");
            this.descriptionField = this.rootVisualElement.Q<TextField>("Description");
        }

        private void CreateTemplate()
        {
            if (!TryGetPath(out var path))
            {
                BLGlobalLogger.LogErrorString("No VisualEffectAsset selected");
                return;
            }

            if (string.IsNullOrWhiteSpace(this.nameField!.value))
            {
                BLGlobalLogger.LogErrorString("No name set");
                return;
            }

            VFXTemplateHelper.TrySetTemplate(path, new VFXTemplateDescriptor
            {
                name = this.nameField.value,
                category = this.categoryField!.value,
                description = this.descriptionField!.value,
            });
        }

        private static bool TryGetPath(out string? path)
        {
            var asset = Selection.activeObject as VisualEffectAsset;
            if (!asset)
            {
                path = null;
                return false;
            }

            path = AssetDatabase.GetAssetPath(asset);
            return !string.IsNullOrEmpty(path);
        }
    }
}
#endif
