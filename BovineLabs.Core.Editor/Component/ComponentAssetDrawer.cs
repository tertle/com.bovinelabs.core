// <copyright file="ComponentAssetDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Component
{
    using System;
    using BovineLabs.Core;
    using BovineLabs.Core.Editor.Helpers;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(ComponentAsset), true)]
    public class ComponentAssetDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var container = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                },
            };

            var field = new PropertyField(property)
            {
                style =
                {
                    flexGrow = 1f,
                },
            };

            var propertyCopy = property.Copy();
            var button = new Button(() => CreateAsset(propertyCopy))
            {
                text = "+",
                style =
                {
                    flexShrink = 0f,
                    marginLeft = 2f,
                },
            };

            container.Add(field);
            container.Add(button);

            return container;
        }

        private static void CreateAsset(SerializedProperty property)
        {
            var assetType = GetAssetType(property);
            if (assetType == null)
            {
                return;
            }

            var path = EditorUtility.SaveFilePanelInProject(
                $"Create {assetType.Name}",
                assetType.Name,
                "asset",
                $"Choose a location for the new {assetType.Name}.");

            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var asset = ScriptableObject.CreateInstance(assetType);
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();

            property.objectReferenceValue = asset;
            property.serializedObject.ApplyModifiedProperties();
        }

        private static Type GetAssetType(SerializedProperty property)
        {
            if (property.objectReferenceValue != null)
            {
                return property.objectReferenceValue.GetType();
            }

            var fieldType = property.GetFieldType();
            if (fieldType == null)
            {
                return null;
            }

            if (fieldType.IsAbstract || !typeof(ComponentAsset).IsAssignableFrom(fieldType))
            {
                return null;
            }

            return fieldType;
        }
    }
}
