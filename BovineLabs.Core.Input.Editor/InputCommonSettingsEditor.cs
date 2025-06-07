// <copyright file="InputCommonSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input.Editor
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Input;
    using BovineLabs.Core.Input.Authoring;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(InputCommonSettings))]
    public class InputCommonSettingsEditor : ElementEditor
    {
        private const string SettingsProperty = "settings";
        private const string DebugSettingsProperty = "debugSettings";

        /// <inheritdoc />
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            if (property.name == SettingsProperty)
            {
                var ve = new VisualElement();
                var button = new Button(this.Refresh) { text = "Find All Input" };
                ve.Add(button);
                ve.Add(CreatePropertyField(property));
                return ve;
            }

            return CreatePropertyField(property);
        }

        private void Refresh()
        {
            var baseType = typeof(IInputSettings);

            var property = this.serializedObject.FindProperty(SettingsProperty);
            var debugProperty = this.serializedObject.FindProperty(DebugSettingsProperty);

            ClearNullReferences(property);
            ClearNullReferences(debugProperty);

            foreach (var type in AppDomain
                .CurrentDomain
                .GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(t => t != baseType)
                .Where(t => t.IsClass && t is { IsInterface: false, IsAbstract: false })
                .Where(t => baseType.IsAssignableFrom(t)))
            {
                var isDebug = type.FullName!.Contains("Debug");

                if (TypeExistsInArray(property, type, out var index))
                {
                    if (isDebug)
                    {
                        property.DeleteArrayElementAtIndex(index);
                    }
                    else
                    {
                        continue;
                    }
                }

                if (TypeExistsInArray(debugProperty, type, out index))
                {
                    if (!isDebug)
                    {
                        debugProperty.DeleteArrayElementAtIndex(index);
                    }
                    else
                    {
                        continue;
                    }
                }

                var obj = (IInputSettings)Activator.CreateInstance(type);
                SerializedProperty element;

                if (isDebug)
                {
                    debugProperty.InsertArrayElementAtIndex(debugProperty.arraySize);
                    element = debugProperty.GetArrayElementAtIndex(debugProperty.arraySize - 1);
                }
                else
                {
                    property.InsertArrayElementAtIndex(property.arraySize);
                    element = property.GetArrayElementAtIndex(property.arraySize - 1);
                }

                element.managedReferenceValue = obj;
            }

            this.serializedObject.ApplyModifiedProperties();
        }

        private static void ClearNullReferences(SerializedProperty property)
        {
            var size = property.arraySize;
            for (var i = size - 1; i >= 0; i--)
            {
                if (property.GetArrayElementAtIndex(i).managedReferenceValue == null)
                {
                    property.DeleteArrayElementAtIndex(i);
                }
            }
        }

        private static bool TypeExistsInArray(SerializedProperty property, Type type, out int index)
        {
            var size = property.arraySize;
            for (index = 0; index < size; index++)
            {
                var element = property.GetArrayElementAtIndex(index);

                if (element.managedReferenceValue.GetType() == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
