// <copyright file="UIDAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.ObjectManagement;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(UIDAttribute))]
    public class UIDAttributeDrawer : PropertyDrawer
    {
        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                return new Label($"{nameof(UIDAttribute)} on non int field");
            }

            var keyedAsset = (UIDAttribute)this.attribute;
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
                var value = evt.newValue is not IUID keyed ? 0 : keyed.ID;
                property.intValue = value;
                property.serializedObject.ApplyModifiedProperties();
            });

            return objectField;
        }

        private static Dictionary<int, IUID> GetBestType(string name)
        {
            var keyedAssets = new Dictionary<int, IUID>();

            var assets = AssetDatabase.FindAssets($"t:{name}");
            foreach (var guid in assets)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);

                var assetsAtPath = AssetDatabase.LoadAllAssetsAtPath(path);

                foreach (var obj in assetsAtPath)
                {
                    if (obj is not IUID asset || asset.GetType().Name != name)
                    {
                        continue;
                    }

                    try
                    {
                        keyedAssets.Add(asset.ID, asset);
                    }
                    catch (Exception ex)
                    {
                        BLGlobalLogger.LogErrorString($"GetBestType failed for {path}");
                        BLGlobalLogger.LogFatal(ex);
                    }
                }
            }

            return keyedAssets;
        }
    }
}
#endif
