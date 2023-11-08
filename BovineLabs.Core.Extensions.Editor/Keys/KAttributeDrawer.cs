// <copyright file="KAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_CONFIG
namespace BovineLabs.Core.Editor.Keys
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Keys;
    using Unity.Mathematics;
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
                var defaultValue = GetDefaultValue((uint)property.intValue, k.Keys, choices);
                var remap = GetRemap(k.Keys, choices);

                var popup = new MaskField(property.displayName, choices, defaultValue);
                popup.RegisterValueChangedCallback(evt =>
                {
                    property.intValue = Remap((uint)evt.newValue, remap);
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

        private static int Remap(uint c, Dictionary<int, int> remap)
        {
            var value = 0;
            while (c != 0)
            {
                var index = math.tzcnt(c);
                var shifted = (uint)(1 << index);
                c ^= shifted;
                if (remap.TryGetValue(index, out var i))
                {
                    value |= 1 << i;
                }
            }

            return value;
        }

        private static Dictionary<int, int> GetRemap(IEnumerable<NameValue> keys, List<string> choices)
        {
            var nameToValue = keys.ToDictionary(key => key.Name, key => key.Value);
            var remap = new Dictionary<int, int>();
            for (var index = 0; index < choices.Count; index++)
            {
                remap.Add(index, nameToValue[choices[index]]);
            }

            return remap;
        }

        private static int GetDefaultValue(uint c, IEnumerable<NameValue> keys, List<string> choices)
        {
            var setup = keys.ToDictionary(key => key.Value, key => key.Name);
            var defaultValue = 0;
            while (c != 0)
            {
                var index = math.tzcnt(c);
                var shifted = (uint)(1 << math.tzcnt(c));
                c ^= shifted;

                if (!setup.TryGetValue(index, out var choice))
                {
                    continue;
                }

                var i = choices.IndexOf(choice);
                defaultValue |= 1 << i;
            }

            return defaultValue;
        }
    }
}
#endif
