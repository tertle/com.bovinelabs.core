// <copyright file="KAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Keys
{
    using System.Linq;
    using BovineLabs.Core.Keys;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(KAttribute), true)]
    public class KAttributeDrawer : PropertyDrawer
    {
        /// <inheritdoc/>
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                return new Label("KAttribute can only be applied to integer fields.");
            }

            var attr = (KAttribute)this.attribute;
            var k = Resources.Load<KSettings>(attr.Settings);

            if (k == null)
            {
                return new Label($"Settings file {attr.Settings} not found");
            }

            if (attr.Flags)
            {
                var choices = k.Keys.Select(s => s.Name).ToList();
                var popup = new MaskField(property.displayName, choices, property.intValue);
                popup.RegisterValueChangedCallback(evt =>
                {
                    property.intValue = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                });
                return popup;
            }
            else
            {
                var current = property.intValue;
                int index = 0;
                for (; index < k.Keys.Count; index++)
                {
                    if (current == k.Keys[index].Value)
                    {
                        break;
                    }
                }

                var choices = k.Keys.Select(s => s.Value).ToList();

                var popup = new PopupField<int>(property.displayName, choices, index, FormatCallback, FormatCallback);
                popup.RegisterValueChangedCallback(evt =>
                {
                    property.intValue = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                });

                return popup;

                string FormatCallback(int i) => k.Keys.FirstOrDefault(key => i == key.Value).Name ?? "[None]";
            }
        }
    }
}
