// <copyright file="KeyedAssetAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(KeyedAssetAttribute))]
    public class KeyedAssetAttributeDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                return new Label($"{nameof(KeyedAssetAttribute)} on non int field");
            }

            var keyedAsset = (KeyedAssetAttribute)this.attribute;
            var assets = GetBestType(keyedAsset.Type);

            if (assets.Count == 0)
            {
                return new Label($"None of {keyedAsset.Type} found");
            }

            var objectField = new ObjectField(property.displayName)
            {
                allowSceneObjects = false,
                objectType = assets.First().Value.GetType(),
            };

            if (assets.TryGetValue(property.intValue, out var asset))
            {
                objectField.value = (ScriptableObject)asset;
            }

            objectField.RegisterValueChangedCallback(evt =>
            {
                var value = evt.newValue is not IKeyedAsset keyed ? 0 : keyed.Key;
                property.intValue = value;
                property.serializedObject.ApplyModifiedProperties();
            });

            return objectField;
        }

        private static Dictionary<int, IKeyedAsset> GetBestType(string name)
        {
            var keyedAssets = new Dictionary<int, IKeyedAsset>();

            var assets = AssetDatabase.FindAssets($"t:{name}");
            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) is not IKeyedAsset asset)
                {
                    Debug.Log("Not all assets");
                    continue;
                }

                try
                {
                    keyedAssets.Add(asset.Key, asset);
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex);
                }
            }

            return keyedAssets;
        }
    }
}
