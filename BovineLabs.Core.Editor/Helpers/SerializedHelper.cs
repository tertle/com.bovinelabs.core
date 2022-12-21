// <copyright file="SerializedHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Helpers
{
    using System.Collections.Generic;
    using UnityEditor;

    public static class SerializedHelper
    {
        public static IEnumerable<SerializedProperty> IterateAllChildren(SerializedObject root, bool siblingProperties = false)
        {
            var iterator = root.GetIterator();

            for (var enterChildren = true; iterator.NextVisible(enterChildren); enterChildren = false)
            {
                if (iterator.propertyPath != "m_Script")
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

        public static IEnumerable<SerializedProperty> GetChildren(SerializedProperty property)
        {
            var currentProperty = property.Copy();
            var nextSiblingProperty = property.Copy();
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
    }
}
