// <copyright file="StableTypeHashAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_2023_3_OR_NEWER
namespace BovineLabs.Core.Editor.Inspectors
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.Editor.UI;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Utility;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(StableTypeHashAttribute))]
    public sealed class StableTypeHashAttributeDrawer : StableTypeHashAttributeBaseDrawer<StableTypeHashAttribute>
    {
        protected override List<SearchView.Item> GenerateItems(StableTypeHashAttribute att)
        {
            var componentTypes = new List<SearchView.Item> { new() { Path = "None", Data = 0UL } };

            foreach (TypeManager.TypeInfo t in TypeManager.AllTypes)
            {
                if (att.OnlyZeroSize && !t.IsZeroSized)
                {
                    continue;
                }

                if (att.OnlyEnableable && !t.TypeIndex.IsEnableable)
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

                if (!CategoryMatch(t.Category, att.Category))
                {
                    continue;
                }

                if (!att.AllowEditorAssemblies && type.Assembly.IsAssemblyEditorAssembly())
                {
                    continue;
                }

                if (!att.AllowUnityNamespace && (type.Namespace != null) && type.Namespace.StartsWith("Unity"))
                {
                    continue;
                }

                if (att.BaseType != null && att.BaseType.Any(baseType => !baseType.IsAssignableFrom(type)))
                {
                    continue;
                }

                componentTypes.Add(new SearchView.Item { Path = t.DebugTypeName.ToString().Replace('.', '/'), Data = t.StableTypeHash });
            }

            return componentTypes;
        }
    }

    public abstract class StableTypeHashAttributeBaseDrawer<T> : PropertyDrawer
        where T : PropertyAttribute
    {
        private static readonly Dictionary<T, List<SearchView.Item>> Attributes = new();

        /// <inheritdoc />
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var stableHashAttribute = (T)this.attribute;

            if (!Attributes.TryGetValue(stableHashAttribute, out var items))
            {
                items = Attributes[stableHashAttribute] = this.GenerateItems(stableHashAttribute);
            }

            var searchElement = new SearchElement(items, string.Empty, property.displayName);
            searchElement.OnSelection += item =>
            {
                var stableTypeHash = (ulong)item.Data!;
                property.longValue = (long)stableTypeHash;
                property.serializedObject.ApplyModifiedProperties();
            };

            var searchButton = searchElement.Q<Button>();
            searchElement.SetText = item => HashToName((ulong)item.Data, searchButton.worldBound.width);

            searchElement.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                searchElement.Text = HashToName((ulong)property.longValue, searchButton.worldBound.width);
            });

            return searchElement;
        }

        protected abstract List<SearchView.Item> GenerateItems(T att);

        protected static bool CategoryMatch(TypeManager.TypeCategory type, StableTypeHashAttribute.TypeCategory category)
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

        private static string HashToName(ulong stableTypeHash, float width)
        {
            var maxLength = width / 7.5f; // Just trial and error what fits best

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
    }
}
#endif
