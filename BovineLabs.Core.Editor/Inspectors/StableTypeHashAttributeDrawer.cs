// <copyright file="StableTypeHashAttributeDrawer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_6000_0_OR_NEWER
namespace BovineLabs.Core.Editor.Inspectors
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.PropertyDrawers;
    using BovineLabs.Core.Utility;
    using Unity.Entities;
    using UnityEditor;

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

}
#endif
