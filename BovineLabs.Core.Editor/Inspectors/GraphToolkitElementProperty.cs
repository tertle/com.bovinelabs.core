// <copyright file="GraphToolkitElementProperty.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_6000_5_OR_NEWER
namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine.UIElements;

    /// <summary> Element property base class for Unity Graph Toolkit inline value editors. </summary>
    public abstract class GraphToolkitElementProperty : ElementProperty
    {
        /// <summary> Marker USS class for roots already configured by this property drawer base. </summary>
        public const string FieldUssClassName = "bl-core-graph-toolkit-element-property";

        /// <summary> Graph Toolkit USS class for model property field roots. </summary>
        public const string GraphToolkitModelPropertyFieldUssClassName = "ge-model-property-field";

        /// <summary> Graph Toolkit USS class for inspector field containers. </summary>
        public const string GraphToolkitInspectorFieldsUssClassName = "ge-inspector-fields";

        /// <summary> Graph Toolkit USS class for node constant editor containers. </summary>
        public const string GraphToolkitNodeConstantEditorUssClassName = "ge-node__constant-editor";

        /// <summary> Graph Toolkit USS class for model property labels. </summary>
        public const string GraphToolkitLabelUssClassName = "ge-model-property-field__label";

        /// <summary> Graph Toolkit USS class for model property input elements. </summary>
        public const string GraphToolkitInputUssClassName = "ge-model-property-field__input";

        private const string UnityPropertyFieldLabelUssClassName = "unity-property-field__label";
        private const string UnityPropertyFieldInputUssClassName = "unity-property-field__input";
        private const float GraphToolkitLabelFontSize = 12;
        private const float GraphToolkitLabelWidthBuffer = 4;

        /// <inheritdoc/>
        protected override sealed ParentTypes ParentType => this.UseFoldout ? ParentTypes.Foldout : ParentTypes.None;

        /// <summary> Gets a value indicating whether this drawer should use the default foldout parent for generic properties. </summary>
        protected virtual bool UseFoldout => true;

        /// <inheritdoc/>
        protected override string GetDisplayName(SerializedProperty property)
        {
            if (TryGetGraphToolkitOwnerString(property.serializedObject.targetObject, out var name, "Title", "DisplayName"))
            {
                return name;
            }

            return base.GetDisplayName(property);
        }

        /// <inheritdoc/>
        protected override string GetTooltip(SerializedProperty property)
        {
            if (TryGetGraphToolkitOwnerString(property.serializedObject.targetObject, out var tooltip, "Tooltip"))
            {
                return tooltip;
            }

            return base.GetTooltip(property);
        }

        /// <inheritdoc/>
        protected override bool PreElementCreation(VisualElement root)
        {
            AlignForGraphToolkit(root);
            return base.PreElementCreation(root);
        }

        /// <summary> Creates a Graph Toolkit-aligned property field for the property. </summary>
        /// <param name="property"> The serialized property to bind. </param>
        /// <returns> The aligned property field. </returns>
        protected static new PropertyField CreatePropertyField(SerializedProperty property)
        {
            return AlignForGraphToolkit(ElementProperty.CreatePropertyField(property));
        }

        /// <summary> Creates a Graph Toolkit-aligned property field for the property. </summary>
        /// <param name="property"> The serialized property to bind. </param>
        /// <param name="serializedObject"> The serialized object to bind against. </param>
        /// <returns> The aligned property field. </returns>
        protected static new PropertyField CreatePropertyField(SerializedProperty property, SerializedObject serializedObject)
        {
            return AlignForGraphToolkit(ElementProperty.CreatePropertyField(property, serializedObject));
        }

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            return AlignForGraphToolkit(base.CreateElement(property));
        }

        /// <summary> Applies Graph Toolkit label and input alignment classes to a visual element tree. </summary>
        /// <param name="element"> The visual element tree to align. </param>
        /// <typeparam name="T"> The root visual element type. </typeparam>
        /// <returns> The input element. </returns>
        protected static T AlignForGraphToolkit<T>(T element)
            where T : VisualElement
        {
            if (element.ClassListContains(FieldUssClassName))
            {
                return element;
            }

            element.AddToClassList(FieldUssClassName);
            ApplyGraphToolkitAlignment(element);
            element.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
            element.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            return element;
        }

        private static void OnAttachToPanel(AttachToPanelEvent evt)
        {
            if (evt.target is not VisualElement root)
            {
                return;
            }

            ApplyGraphToolkitAlignment(root);
            root.schedule.Execute(() => ApplyGraphToolkitAlignment(root)).ExecuteLater(0);
        }

        private static void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (evt.target is VisualElement root)
            {
                ApplyGraphToolkitAlignment(root);
            }
        }

        private static void ApplyGraphToolkitAlignment(VisualElement root)
        {
            var owningModelPropertyField = FindOwningModelPropertyField(root);
            owningModelPropertyField?.AddToClassList(PropertyField.ussClassName);

            root
                .Query<Label>(className: BaseField<string>.labelUssClassName)
                .ForEach(label =>
                {
                    label.AddToClassList(GraphToolkitLabelUssClassName);
                    label.AddToClassList(UnityPropertyFieldLabelUssClassName);
                });

            root
                .Query<VisualElement>(className: BaseField<string>.inputUssClassName)
                .ForEach(input =>
                {
                    input.AddToClassList(GraphToolkitInputUssClassName);
                    input.AddToClassList(UnityPropertyFieldInputUssClassName);
                });

            ApplyGraphToolkitLabelWidths(root);
        }

        private static void ApplyGraphToolkitLabelWidths(VisualElement root)
        {
            var scope = FindLabelWidthScope(root);

            if (scope.panel == null)
            {
                return;
            }

            var labels = new List<Label>();
            scope.Query<Label>(className: GraphToolkitLabelUssClassName).ForEach(labels.Add);
            var maxLabelRight = 0f;

            foreach (var label in labels)
            {
                if (!TryGetLabelPosition(scope, label, out var x))
                {
                    continue;
                }

                var textSize = label.MeasureTextSize(
                    label.text,
                    float.NaN,
                    VisualElement.MeasureMode.Undefined,
                    float.NaN,
                    VisualElement.MeasureMode.Undefined,
                    GraphToolkitLabelFontSize);

                var labelRight = x + textSize.x;

                if (labelRight > maxLabelRight)
                {
                    maxLabelRight = labelRight;
                }
            }

            if (maxLabelRight <= 0 || float.IsNaN(maxLabelRight) || float.IsInfinity(maxLabelRight))
            {
                return;
            }

            foreach (var label in labels)
            {
                if (!TryGetLabelPosition(scope, label, out var x))
                {
                    continue;
                }

                label.style.minWidth = maxLabelRight + GraphToolkitLabelWidthBuffer - x;
            }
        }

        private static VisualElement FindOwningModelPropertyField(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (current.ClassListContains(GraphToolkitModelPropertyFieldUssClassName))
                {
                    return current;
                }
            }

            return null;
        }

        private static VisualElement FindLabelWidthScope(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
            {
                if (current.ClassListContains(GraphToolkitInspectorFieldsUssClassName)
                    || current.ClassListContains(GraphToolkitNodeConstantEditorUssClassName))
                {
                    return current;
                }
            }

            return FindOwningModelPropertyField(element) ?? element;
        }

        private static bool TryGetGraphToolkitOwnerString(object targetObject, out string value, params string[] propertyNames)
        {
            value = string.Empty;

            if (!IsGraphToolkitWrapper(targetObject))
            {
                return false;
            }

            if (!TryGetPropertyValue(targetObject, "Owner", out var owner) || owner == null)
            {
                return false;
            }

            foreach (var propertyName in propertyNames)
            {
                if (TryGetNonEmptyStringProperty(owner, propertyName, out value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsGraphToolkitWrapper(object targetObject)
        {
            if (targetObject == null)
            {
                return false;
            }

            for (var type = targetObject.GetType(); type != null; type = type.BaseType)
            {
                if (type.Name.StartsWith("Wrapper_", StringComparison.Ordinal)
                    || type.FullName == "Unity.GraphToolkit.Editor.FieldWrapper"
                    || (type.FullName?.StartsWith("Unity.GraphToolkit.Editor.FieldWrapper`", StringComparison.Ordinal) ?? false))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryGetNonEmptyStringProperty(object source, string propertyName, out string value)
        {
            value = string.Empty;

            if (!TryGetPropertyValue(source, propertyName, out var propertyValue) || propertyValue is not string text || string.IsNullOrEmpty(text))
            {
                return false;
            }

            value = text;
            return true;
        }

        private static bool TryGetPropertyValue(object source, string propertyName, out object value)
        {
            value = null;

            var property = FindProperty(source.GetType(), propertyName);
            if (property == null)
            {
                return false;
            }

            value = property.GetValue(source);
            return true;
        }

        private static PropertyInfo FindProperty(Type type, string propertyName)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;

            for (var current = type; current != null; current = current.BaseType)
            {
                foreach (var property in current.GetProperties(flags))
                {
                    if (property.GetIndexParameters().Length != 0)
                    {
                        continue;
                    }

                    if (property.Name == propertyName || property.Name.EndsWith("." + propertyName, StringComparison.Ordinal))
                    {
                        return property;
                    }
                }
            }

            return null;
        }

        private static bool TryGetLabelPosition(VisualElement scope, Label label, out float x)
        {
            x = 0;

            if (label.parent == null)
            {
                return false;
            }

            if (scope.panel == null || label.panel != scope.panel)
            {
                return false;
            }

            x = label.parent.ChangeCoordinatesTo(scope, label.localBound.position).x;
            return !float.IsNaN(x) && !float.IsInfinity(x);
        }
    }
}
#endif
