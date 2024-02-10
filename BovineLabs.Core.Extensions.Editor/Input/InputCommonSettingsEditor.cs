// <copyright file="InputCommonSettingsEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Editor.Input
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Authoring.Input;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Input;
    using UnityEditor;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(InputCommonSettings))]
    public class InputCommonSettingsEditor : ElementEditor
    {
        private const string SettingsProperty = "settings";

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            if (property.name == SettingsProperty)
            {
                var ve = new VisualElement();
                var button = new Button(this.Refresh) { text = "Find All Input" };
                ve.Add(button);
                ve.Add(base.CreateElement(property));
                return ve;
            }

            return base.CreateElement(property);
        }

        private void Refresh()
        {
            var baseType = typeof(IInputSettings);

            var property = this.serializedObject.FindProperty(SettingsProperty);

            ClearNullReferences(property);

            foreach (var type in AppDomain.CurrentDomain.GetAssemblies()
                         .SelectMany(s => s.GetTypes())
                         .Where(t => t != baseType)
                         .Where(t => t.IsClass && t is { IsInterface: false, IsAbstract: false })
                         .Where(t => baseType.IsAssignableFrom(t)))
            {
                if (TypeExistsInArray(property, type))
                {
                    continue;
                }

                property.InsertArrayElementAtIndex(property.arraySize);
                var element = property.GetArrayElementAtIndex(property.arraySize - 1);
                element.managedReferenceValue = (IInputSettings)Activator.CreateInstance(type);
            }

            this.serializedObject.ApplyModifiedProperties();
        }

        private static void ClearNullReferences(SerializedProperty property)
        {
            var size = property.arraySize;
            for (var i = size - 1; i >= 0; i--)
            {
                var element = property.GetArrayElementAtIndex(i);
                if (property.GetArrayElementAtIndex(i).managedReferenceValue == null)
                {
                    property.DeleteArrayElementAtIndex(i);
                }
            }
        }

        private static bool TypeExistsInArray(SerializedProperty property, Type type)
        {
            var size = property.arraySize;
            for (var i = 0; i < size; i++)
            {
                var element = property.GetArrayElementAtIndex(i);

                if (element.managedReferenceValue.GetType() == type)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
#endif
