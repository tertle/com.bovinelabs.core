// <copyright file="KAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Keys
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Keys;
    using UnityEditor;
    using UnityEngine;

    [CustomPropertyDrawer(typeof(KAttribute), true)]
    public class KAttributeDrawer : PropertyDrawer
    {
        private static readonly Dictionary<string, KSettings> SettingsMap = new(StringComparer.OrdinalIgnoreCase);

        public static KSettings? GetSettingsFile(KAttribute attr)
        {
            if (!SettingsMap.TryGetValue(attr.Settings, out var k))
            {
                k = Resources.Load<KSettings>(attr.Settings);

                if (k != null)
                {
                    SettingsMap[attr.Settings] = k;
                }
            }

            return k;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var attr = (KAttribute)this.attribute;

            if (property.propertyType != SerializedPropertyType.Integer)
            {
                EditorGUI.LabelField(position, "KAttribute can only be applied to integer fields.");
                return;
            }

            var k = GetSettingsFile(attr);

            if (k == null)
            {
                EditorGUI.LabelField(position, $"Settings file {attr.Settings} not found");
                return;
            }

            var current = property.intValue;
            int index;
            for (index = 0; index < k.Keys.Length; index++)
            {
                if (k.Keys[index].Value == current)
                {
                    break;
                }
            }

            EditorGUI.BeginProperty(position, label, property);
            var newIndex = EditorGUI.Popup(position, label, index, k.Keys.Select(s => new GUIContent(s.Name)).ToArray());
            if (newIndex != index)
            {
                property.intValue = k.Keys[newIndex].Value;
            }

            EditorGUI.EndProperty();
        }
    }
}
