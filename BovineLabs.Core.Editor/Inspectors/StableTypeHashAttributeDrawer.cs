// <copyright file="StableTypeHashAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Utility;
    using Unity;
    using Unity.Entities;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(StableTypeHashAttribute))]
    public sealed class StableTypeHashAttributeDrawer : StableTypeHashAttributeBaseDrawer<StableTypeHashAttribute>
    {
        protected override List<SearchView.Item> GenerateItems(StableTypeHashAttribute att)
        {
            var componentTypes = new List<SearchView.Item>
            {
                new()
                {
                    Path = "None",
                    Data = 0UL,
                },
            };

            if (att.OnlyZeroSize)
            {
                if (att.OnlySize)
                {
                    Debug.LogError("OnlyZeroSize && OnlySize will return no results");
                    return componentTypes;
                }

                if (att.Category == StableTypeHashAttribute.TypeCategory.BufferData)
                {
                    Debug.LogError("OnlyZeroSize && Buffer category will return no results");
                    return componentTypes;
                }
            }

            foreach (var t in TypeManager.AllTypes)
            {
                if (att.OnlyZeroSize && !t.IsZeroSized)
                {
                    continue;
                }

                if (att.OnlySize && t.IsZeroSized)
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

                if (!att.AllowEditorAssemblies && (type.Assembly.IsAssemblyEditorAssembly() || type.Assembly.IsTestEditorAssembly()))
                {
                    continue;
                }

                if (!att.AllowUnityNamespace && type.Namespace != null && type.Namespace.StartsWith("Unity"))
                {
                    continue;
                }

                if (att.BaseType != null && att.BaseType.Any(baseType => !baseType.IsAssignableFrom(type)))
                {
                    continue;
                }

                componentTypes.Add(new SearchView.Item
                {
                    Path = t.DebugTypeName.ToString().Replace('.', '/'),
                    Data = t.StableTypeHash,
                });
            }

            return componentTypes;
        }
    }
}
