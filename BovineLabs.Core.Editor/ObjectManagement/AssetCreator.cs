// <copyright file="AssetCreator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.SearchWindow;
    using BovineLabs.Core.Editor.UI;
    using BovineLabs.Core.ObjectManagement;
    using BovineLabs.Core.Utility;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class AssetCreator
    {
        private readonly string path;
        private readonly SerializedObject serializedObject;
        private readonly SerializedProperty serializedProperty;
        private readonly Type type;
        private readonly bool isAbstract;
        private readonly List<SearchView.Item> items = new();

        private ListView listView;

        private AutoRefAttribute attribute;

        public AssetCreator(SerializedObject serializedObject, SerializedProperty serializedProperty, Type type)
        {
            this.attribute = TryGetAttribute(serializedObject, serializedProperty, type);

            this.serializedObject = serializedObject;
            this.serializedProperty = serializedProperty;
            this.type = type;
            this.serializedProperty.isExpanded = false;

            this.isAbstract = this.type.IsAbstract;

            if (this.isAbstract)
            {
                foreach (var i in ReflectionUtility.GetAllImplementations(type))
                {
                    this.items.Add(new SearchView.Item
                    {
                        Path = i.Name,
                        Data = i,
                    });
                }

            }

            this.Element = PropertyUtil.CreateProperty(serializedProperty, this.serializedObject);

            if (this.attribute != null)
            {
                this.Element.RegisterCallback<GeometryChangedEvent>(this.Init);
                this.Element.AddManipulator(new ContextualMenuManipulator(this.MenuBuilder));
                this.path = this.isAbstract ? OMUtility.GetDefaultPathWithoutFileName(this.attribute) : OMUtility.GetDefaultPath(this.attribute);
            }
        }

        public PropertyField Element { get; }

        private static AutoRefAttribute TryGetAttribute(SerializedObject serializedObject, SerializedProperty serializedProperty, Type type)
        {
            var attributes = type.GetCustomAttributes<AutoRefAttribute>(true).ToArray();
            var attribute = attributes.FirstOrDefault(a => a.ManagerType == serializedObject.targetObject.name && a.FieldName == serializedProperty.name);

            if (attribute == null && attributes.Length == 0)
            {
                BLGlobalLogger.LogErrorString(
                    $"Type {type} is using AssetCreator but without {nameof(AutoRefAttribute)} so the item will not be added to the object.");
            }
            else if (attribute == null)
            {
                BLGlobalLogger.LogErrorString(
                    $"Type {type} is using AssetCreator but no {nameof(AutoRefAttribute)} targets {serializedObject.targetObject.name}.{serializedProperty.name}.");
            }

            return attribute;
        }

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

                if (this.isAbstract)
                {
                    // Remove the elements unity just force added, they will be added back properly via autoref
                    this.serializedObject.Update();
                    this.serializedProperty.arraySize -= count;
                    this.serializedObject.ApplyModifiedPropertiesWithoutUndo();

                    var searchWindow = SearchWindow.Create();

                    searchWindow.Items = this.items;
                    searchWindow.OnSelection += item =>
                    {
                        var t = (Type)item.Data;
                        var p = Path.Combine(this.path!, item.Name + ".asset");
                        Create(count, t, p);
                    };

                    var button = this.listView.Q<Button>("unity-list-view__add-button");

                    var screenPosition = VisualElementUtil.GetScreenPosition(button);
                    var size = new Rect(screenPosition.x, screenPosition.y + button.worldBound.height, 400, 400);
                    searchWindow.position = size;
                    searchWindow.ShowPopup();
                }
                else
                {
                    Create(count, this.type, this.path!);
                }

                return;

                static void Create(int count, Type selectedType, string path)
                {
                    for (var i = 0; i < count; i++)
                    {
                        OMUtility.CreateInstance(selectedType, path);
                    }
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
