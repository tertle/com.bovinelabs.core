// <copyright file="TypeAssetEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Component
{
    using System;
    using BovineLabs.Core.Editor.Inspectors;
    using UnityEditor;
    using UnityEditor.Search;
    using UnityEngine;
    using UnityEngine.Search;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(TypeAsset))]
    public class TypeAssetEditor : ElementEditor
    {
        private Button button;

        protected virtual string SearchQuery => "unmanaged=true";

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            return property.name switch
            {
                "typeName" => this.button = new Button(() => this.Search(property))
                {
                    text = FormatName(property.stringValue),
                    tooltip = property.stringValue,
                },
                _ => base.CreateElement(property),
            };
        }

        private void Search(SerializedProperty property)
        {
            var context = SearchService.CreateContext(TypeAsset.SearchProviderType, this.SearchQuery);

            var viewState =
                new SearchViewState(context,
                    SearchViewFlags.ListView | SearchViewFlags.OpenInBuilderMode | SearchViewFlags.DisableSavedSearchQuery | SearchViewFlags.CompactView)
                {
                    windowTitle = new GUIContent("Type Selector"),
                    title = "Select Type",
                    position = SearchUtils.GetMainWindowCenteredPosition(new Vector2(600, 400)),
                    selectHandler = (item, canceled) =>
                    {
                        if (canceled || item == null)
                        {
                            return;
                        }

                        property.stringValue = item.data as string ?? string.Empty;
                        property.serializedObject.ApplyModifiedProperties();
                        this.button!.text = FormatName(property.stringValue);
                        this.button.tooltip = property.stringValue;
                    },
                };

            SearchService.ShowPicker(viewState);
        }

        private static string FormatName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName))
            {
                return "Select a type";
            }

            var type = Type.GetType(typeName);
            if (type == null)
            {
                return $"Missing type {typeName}";
            }

            return type.Name;
        }
    }
}
