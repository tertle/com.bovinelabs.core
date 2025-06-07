// <copyright file="AssetCreator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.ObjectManagement;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class AssetCreator
    {
        private readonly string? path;
        private readonly SerializedObject serializedObject;
        private readonly SerializedProperty serializedProperty;
        private readonly Type type;

        private ListView? listView;

        private AutoRefAttribute? attribute;

        public AssetCreator(SerializedObject serializedObject, SerializedProperty serializedProperty, Type type)
        {
            this.attribute = TryGetAttribute(serializedObject, serializedProperty, type);

            this.serializedObject = serializedObject;
            this.serializedProperty = serializedProperty;
            this.type = type;
            this.serializedProperty.isExpanded = false;

            this.Element = PropertyUtil.CreateProperty(serializedProperty, this.serializedObject);

            if (this.attribute != null)
            {
                this.Element.RegisterCallback<GeometryChangedEvent>(this.Init);
                this.Element.AddManipulator(new ContextualMenuManipulator(this.MenuBuilder));
                this.path = OMUtility.GetDefaultPath(this.attribute);
            }
        }

        private static AutoRefAttribute? TryGetAttribute(SerializedObject serializedObject, SerializedProperty serializedProperty, Type type)
        {
            var attribute = type.GetCustomAttribute<AutoRefAttribute>();

            if (attribute == null)
            {
                BLGlobalLogger.LogErrorString(
                    $"Type {type} is using AssetCreator but without {nameof(AutoRefAttribute)} so the item will not be added to the object.");
            }
            else if (attribute.ManagerType != serializedObject.targetObject.name)
            {
                BLGlobalLogger.LogErrorString($"Type {type} is using AssetCreator but the {nameof(AutoRefAttribute)} targets a different manager.");
            }
            else if (attribute.FieldName != serializedProperty.name)
            {
                BLGlobalLogger.LogErrorString($"Type {type} is using AssetCreator but the {nameof(AutoRefAttribute)} targets a different field.");
            }

            return attribute;
        }

        public PropertyField Element { get; }

        private void Init(GeometryChangedEvent evt)
        {
            this.listView = this.Element.Q<ListView>();
            if (this.listView == null)
            {
                return;
            }

            this.Element.UnregisterCallback<GeometryChangedEvent>(this.Init);

            var removeButton = this.listView.Q<Button>("unity-list-view__remove-button");
            removeButton.parent.Remove(removeButton);

            this.listView.showBoundCollectionSize = false;

            this.listView.itemsAdded += ints =>
            {
                var count = ints.Count();
                for (var i = 0; i < count; i++)
                {
                    OMUtility.CreateInstance(this.type, this.path!);
                }
            };

            this.listView.Q<VisualElement>("unity-content-container").SetEnabled(false);
        }

        private void MenuBuilder(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Remove Missing", _ =>
            {
                for (var i = this.serializedProperty.arraySize - 1; i >= 0; i--)
                {
                    if (this.serializedProperty.GetArrayElementAtIndex(i).objectReferenceValue == null)
                    {
                        this.serializedProperty.DeleteArrayElementAtIndex(i);
                    }
                }

                this.serializedObject.ApplyModifiedProperties();
            });
        }
    }

    public class AssetCreator<T> : AssetCreator
        where T : ScriptableObject
    {
        public AssetCreator(
            SerializedObject serializedObject, SerializedProperty serializedProperty)
            : base(serializedObject, serializedProperty, typeof(T))
        {
        }
    }
}
