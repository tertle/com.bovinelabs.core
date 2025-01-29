// <copyright file="BitFieldAttributeEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Inspectors;
    using Unity.Mathematics;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;
    using MaskField = UnityEditor.UIElements.MaskField;

    public abstract class BitFieldAttributeEditor<T> : PropertyDrawer
        where T : PropertyAttribute, IBitFieldAttribute
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType != SerializedPropertyType.Integer)
            {
                return new Label($"{typeof(T).Name} can only be applied to integer fields.");
            }

            var attr = (T)this.attribute;
            var keyValues = this.GetKeyValues(attr);

            if (keyValues == null)
            {
                return new Label($"{typeof(T).Name} is misconfigured or missing data.");
            }

            var k = keyValues.OrderBy(a => a.Value).ToArray();

            if (attr.Flags)
            {
                var choices = k.Select(s => s.Name).ToList();
                var defaultValue = GetDefaultValue(property.ulongValue, k, choices);
                var remap = GetRemap(k, choices);

                var popup = new MaskField(property.displayName, choices, defaultValue);
                popup.AddToClassList(BaseField<int>.alignedFieldUssClassName);
                popup.RegisterValueChangedCallback(evt =>
                {
                    property.ulongValue = Remap(evt.newValue, remap);
                    property.serializedObject.ApplyModifiedProperties();
                });

                return popup;
            }
            else
            {
                var current = property.intValue;
                var index = 0;
                for (; index < k.Length; index++)
                {
                    if (current == k[index].Value)
                    {
                        break;
                    }
                }

                var choices = k.Select(s => s.Value).ToList();

                var popup = new PopupField<int>(property.displayName, choices, index, FormatCallback, FormatCallback);
                popup.AddToClassList(BaseField<int>.alignedFieldUssClassName);
                popup.RegisterValueChangedCallback(evt =>
                {
                    property.intValue = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                });

                return popup;

                string FormatCallback(int i)
                {
                    return k.FirstOrDefault(key => i == key.Value).Name ?? "[None]";
                }
            }
        }

        protected abstract IEnumerable<(string Name, int Value)>? GetKeyValues(T attr);

        private static ulong Remap(int c, Dictionary<int, int> remap)
        {
            var value = 0ul;
            while (c != 0)
            {
                var index = math.tzcnt(c);
                var shifted = 1 << index;
                c ^= shifted;
                if (remap.TryGetValue(index, out var i))
                {
                    value |= 1ul << i;
                }
            }

            return value;
        }

        private static Dictionary<int, int> GetRemap(IEnumerable<(string Name, int Value)> data, IReadOnlyList<string> choices)
        {
            var nameToValue = data.ToDictionary(key => key.Name, key => key.Value);
            var remap = new Dictionary<int, int>();
            for (var index = 0; index < choices.Count; index++)
            {
                remap.Add(index, nameToValue[choices[index]]);
            }

            return remap;
        }

        private static int GetDefaultValue(ulong c, IEnumerable<(string Name, int Value)> data, IList<string> choices)
        {
            var setup = data.ToDictionary(key => key.Value, key => key.Name);
            var defaultValue = 0;
            while (c != 0)
            {
                var index = math.tzcnt(c);
                var shifted = 1ul << math.tzcnt(c);
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
