// <copyright file="ObjectGroupUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using UnityEditor;
    using UnityEngine;

    public static class ObjectGroupUtil
    {
        public static void UpdateGroup(SerializedObject objectGroup, SerializedObject objectDefinition)
        {
            var categories = objectDefinition.FindProperty("categories").intValue;

            foreach (var targetObject in objectDefinition.targetObjects)
            {
                var serializedProperty = objectGroup.FindProperty("definitions");

                var index = IndexOf(serializedProperty, targetObject);
                var changed = false;

                var autoGroup = objectGroup.FindProperty("autoGroups").intValue;

                if ((categories & autoGroup) != 0)
                {
                    if (index == -1)
                    {
                        index = serializedProperty.arraySize;
                        serializedProperty.InsertArrayElementAtIndex(index);
                        serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue = targetObject;
                        changed = true;
                    }
                }
                else
                {
                    if (index != -1)
                    {
                        serializedProperty.DeleteArrayElementAtIndex(index);
                        changed = true;
                    }
                }

                if (changed)
                {
                    objectGroup.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.SaveAssetIfDirty(objectGroup.targetObject);
                }
            }
        }

        private static int IndexOf(SerializedProperty property, Object objectDefinition)
        {
            for (var i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == objectDefinition)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
#endif
