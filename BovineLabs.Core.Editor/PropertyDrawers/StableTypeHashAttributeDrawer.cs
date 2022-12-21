// <copyright file="StableTypeHashAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.PropertyDrawers
{
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Utility;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(StableTypeHashAttribute))]
    public class StableTypeHashAttributeDrawer : PropertyDrawer
    {
        private static readonly Dictionary<StableTypeHashAttribute, List<SearchView.Item>> Attributes = new();

        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var stableHashAttribute = (StableTypeHashAttribute)this.attribute;

            if (!Attributes.TryGetValue(stableHashAttribute, out var items))
            {
                items = Attributes[stableHashAttribute] = GenerateItems(stableHashAttribute);
            }

            var parent = new VisualElement { style = { flexDirection = FlexDirection.Row } };

            var label = new Label { text = property.displayName };
            label.AddToClassList("unity-base-field__label");
            parent.Add(label);

            var componentButton = new Button { style = { flexGrow = 1 } };
            componentButton.clicked += () =>
            {
                var searchWindow = SearchWindow.Create();

                searchWindow.Title = "Components";
                searchWindow.Items = items;
                searchWindow.OnSelection += item =>
                {
                    var stableTypeHash = (ulong)item.Data;
                    property.longValue = (long)stableTypeHash;
                    property.serializedObject.ApplyModifiedProperties();

                    componentButton.text = HashToName(stableTypeHash, componentButton.worldBound.width);
                };

                var rect = EditorWindow.focusedWindow.position;
                var worldBounds = label.worldBound;
                var buttonBounds = componentButton.worldBound;

                var size = new Rect(rect.x + worldBounds.x, rect.y + worldBounds.y + worldBounds.height, worldBounds.width + buttonBounds.width, 315);
                searchWindow.position = size;
                searchWindow.ShowPopup();
            };

            componentButton.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                componentButton.text = HashToName((ulong)property.longValue, componentButton.worldBound.width);
            });

            parent.Add(componentButton);
            return parent;
        }

        /// <inheritdoc />
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, property, label, true);
        }

        private static string HashToName(ulong stableTypeHash, float width)
        {
            var maxLength = width / 6.8f; // Just trial and error what fits best

            var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(stableTypeHash);
            var type = typeIndex != -1 ? TypeManager.GetType(typeIndex) : null;

            var name = type == null ? "None" : type.ToString();

            if (name.Length < maxLength)
            {
                return name;
            }

            // We limit size to fit in the inspector window
            var parts = name.Split('.');
            name = parts[^1]; // always include the name at minimum
            var length = name.Length;

            for (var i = parts.Length - 2; i >= 0; i--)
            {
                length += parts[i].Length + 1; // +1 for .
                if (length > maxLength)
                {
                    return name;
                }

                name = parts[i] + "." + name;
            }

            return name;
        }

        private static List<SearchView.Item> GenerateItems(StableTypeHashAttribute attribute)
        {
            var componentTypes = new List<SearchView.Item>();

            foreach (var t in TypeManager.AllTypes)
            {
                if (attribute.OnlyZeroSize && !t.IsZeroSized)
                {
                    continue;
                }

                if (attribute.OnlyEnableable && !t.TypeIndex.IsEnableable)
                {
                    continue;
                }

                if (t.TypeIndex.IsManagedComponent)
                {
                    continue;
                }

                var type = t.Type;

                if (type == null)
                {
                    continue;
                }

                if (!CategoryMatch(t.Category, attribute.Category))
                {
                    continue;
                }

                if (!attribute.AllowEditorAssemblies && type.Assembly.IsAssemblyEditorAssembly())
                {
                    continue;
                }

                if (!attribute.AllowUnityNamespace && (type.Namespace != null) && type.Namespace.StartsWith("Unity"))
                {
                    continue;
                }

                componentTypes.Add(new SearchView.Item { Path = t.DebugTypeName.ToString(), Data = t.StableTypeHash });
            }

            return componentTypes;
        }

        private static bool CategoryMatch(TypeManager.TypeCategory type, StableTypeHashAttribute.TypeCategory category)
        {
            if (category == StableTypeHashAttribute.TypeCategory.None)
            {
                return true;
            }

            switch (type)
            {
                case TypeManager.TypeCategory.ComponentData:
                    return (category & StableTypeHashAttribute.TypeCategory.ComponentData) != 0;
                case TypeManager.TypeCategory.BufferData:
                    return (category & StableTypeHashAttribute.TypeCategory.BufferData) != 0;
                case TypeManager.TypeCategory.ISharedComponentData:
                    return (category & StableTypeHashAttribute.TypeCategory.SharedComponentData) != 0;
                case TypeManager.TypeCategory.EntityData:
                case TypeManager.TypeCategory.UnityEngineObject:
                default:
                    return false;
            }
        }
    }
}
