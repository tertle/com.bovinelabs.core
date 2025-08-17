// <copyright file="ComponentAssetEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Component
{
    using BovineLabs.Core.Editor.Inspectors;
    using Unity.Entities;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    [CustomEditor(typeof(ComponentAsset))]
    public class ComponentAssetEditor : ElementEditor
    {
        private SerializedProperty? componentNameProperty;

        /// <inheritdoc/>
        protected override VisualElement? CreateElement(SerializedProperty property)
        {
            switch (property.name)
            {
                case "componentName":
                {
                    this.componentNameProperty = property;
                    return CreatePropertyField(property);
                }

                case "component":
                {
                    var componentField = CreatePropertyField(property);
                    componentField.RegisterValueChangeCallback(this.ComponentChanged);
                    return componentField;
                }
            }

            return base.CreateElement(property);
        }

        private void ComponentChanged(SerializedPropertyChangeEvent evt)
        {
            var stableTypeHash = evt.changedProperty.ulongValue;
            string compName;

            if (stableTypeHash == 0)
            {
                compName = string.Empty;
            }
            else
            {
                var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(stableTypeHash);

                if (typeIndex == TypeIndex.Null)
                {
                    return;
                }

                compName = TypeManager.GetType(typeIndex)?.FullName ?? string.Empty;
            }

            this.componentNameProperty!.stringValue = compName;
            this.componentNameProperty.serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
