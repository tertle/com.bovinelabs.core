// <copyright file="SerializedHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using BovineLabs.Core.Utility;
    using UnityEditor;

    public static class SerializedHelper
    {
        public static IEnumerable<SerializedProperty> IterateAllChildren(SerializedObject root, bool includeScript, bool siblingProperties = false)
        {
            var iterator = root.GetIterator();

            for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (includeScript || iterator.propertyPath != "m_Script")
                {
                    yield return iterator.Copy();

                    if (siblingProperties)
                    {
                        foreach (var child in GetChildren(iterator))
                        {
                            yield return child;
                        }
                    }
                }
            }
        }

        public static IEnumerable<SerializedProperty> IterateAllChildrenAndFlatten(SerializedObject root)
        {
            var iterator = root.GetIterator();
            return IterateAllChildrenAndFlatten(iterator);
        }

        public static IEnumerable<SerializedProperty> IterateAllChildrenAndFlatten(SerializedProperty iterator)
        {
            for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (iterator.propertyPath != "m_Script")
                {
                    if (iterator.isArray)
                    {
                        yield return iterator.Copy();
                    }
                    else
                    {
                        if (iterator.propertyType != SerializedPropertyType.Generic)
                        {
                            yield return iterator.Copy();
                        }

                        if (iterator.propertyType != SerializedPropertyType.ObjectReference) // probably a few more things here
                        {
                            foreach (var child in GetChildren(iterator))
                            {
                                yield return child;
                            }
                        }
                    }
                }
            }
        }

        public static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property, bool skipSingleRoot = false)
        {
            var rootProperty = skipSingleRoot && TryGetSingleChildRoot(property, out var singleChildRoot) ? singleChildRoot : property;
            var currentProperty = rootProperty.Copy();
            var nextSiblingProperty = rootProperty.Copy();
            nextSiblingProperty.Next(false);

            if (currentProperty.Next(true))
            {
                do
                {
                    if (SerializedProperty.EqualContents(currentProperty, nextSiblingProperty))
                    {
                        yield break;
                    }

                    yield return currentProperty.Copy();
                }
                while (currentProperty.Next(false));
            }
        }

        public static bool TryGetSingleChildRoot(SerializedProperty property, out SerializedProperty childRoot)
        {
            childRoot = null;

            var nextSiblingProperty = property.Copy();
            nextSiblingProperty.Next(false);

            var firstChildProperty = property.Copy();
            if (!firstChildProperty.Next(true) || SerializedProperty.EqualContents(firstChildProperty, nextSiblingProperty))
            {
                return false;
            }

            var secondChildProperty = firstChildProperty.Copy();
            if (secondChildProperty.Next(false) && !SerializedProperty.EqualContents(secondChildProperty, nextSiblingProperty))
            {
                return false;
            }

            if (firstChildProperty.propertyType != SerializedPropertyType.Generic || firstChildProperty.isArray)
            {
                return false;
            }

            var childRootNextSiblingProperty = firstChildProperty.Copy();
            childRootNextSiblingProperty.Next(false);

            var grandChildProperty = firstChildProperty.Copy();
            if (!grandChildProperty.Next(true) || SerializedProperty.EqualContents(grandChildProperty, childRootNextSiblingProperty))
            {
                return false;
            }

            childRoot = firstChildProperty.Copy();
            return true;
        }

        public static Type GetFieldType(this SerializedProperty property)
        {
            var fi = GetFieldInfo(property);
            return fi?.FieldType ?? null;
        }

        // This only works on root objects.
        public static FieldInfo GetFieldInfo(this SerializedProperty property)
        {
            var parentType = property.serializedObject.targetObject.GetType();
            return parentType.GetFieldInBase(property.propertyPath);
        }
    }
}
