// <copyright file="AssetWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.AssetWindow
{
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class AssetWindow : EditorWindow
    {
        private TextField? textField;
        private Label? result;

        [MenuItem("BovineLabs/Tools/Asset", priority = 1015)]
        private static void ShowWindow()
        {
            // Get existing open window or if none, make a new one:
            var window = GetWindow<AssetWindow>();
            window.titleContent = new GUIContent("BovineLabs");
            window.Show();
        }

        private void OnEnable()
        {
            this.textField = new TextField { label = "guid" };
            var button = new Button(this.Search) { text = "Find" };
            this.result = new Label();

            this.rootVisualElement.Add(this.textField);
            this.rootVisualElement.Add(button);
            this.rootVisualElement.Add(this.result);
        }

        private void Search()
        {
            var guid = this.textField!.value;

            var path = string.IsNullOrWhiteSpace(guid) ? string.Empty : AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrWhiteSpace(path))
            {
                path = "Not Found";
            }

            this.result!.text = path;
        }
    }
}
