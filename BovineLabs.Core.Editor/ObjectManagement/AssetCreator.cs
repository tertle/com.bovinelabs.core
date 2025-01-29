// <copyright file="AssetCreator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Editor.Inspectors;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.ObjectManagement;
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
        private readonly string directoryKey;
        private readonly string defaultDirectory;
        private readonly string defaultFileName;
        private ListView? listView;

        public AssetCreator(
            SerializedObject serializedObject, SerializedProperty serializedProperty, Type type, string directoryKey, string defaultDirectory,
            string defaultFileName)
        {
            if (type.GetCustomAttribute<AutoRefAttribute>() == null)
            {
                Debug.LogError($"Type {type} is using AssetCreator but without {nameof(AutoRefAttribute)} so the item will not be added to the object.");
            }

            this.serializedObject = serializedObject;
            this.serializedProperty = serializedProperty;
            this.type = type;
            this.directoryKey = directoryKey;
            this.defaultDirectory = defaultDirectory;
            this.defaultFileName = defaultFileName;
            this.serializedProperty.isExpanded = false;

            this.Element = PropertyUtil.CreateProperty(serializedProperty, this.serializedObject);
            this.path = this.GetDirectory();

            this.Element.RegisterCallback<GeometryChangedEvent>(this.Init);
            this.Element.AddManipulator(new ContextualMenuManipulator(this.MenuBuilder));
        }

        public PropertyField Element { get; }

        protected virtual void Initialize(object instance)
        {
        }

        private string GetDirectory()
        {
            var directory = EditorSettingsUtility.GetAssetDirectory(this.directoryKey, this.defaultDirectory);
            return Path.Combine(directory, this.defaultFileName);
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
                for (var i = 0; i < count; i++)
                {
                    var instance = ScriptableObject.CreateInstance(this.type);
                    this.Initialize(instance);
                    AssetDatabase.CreateAsset(instance, AssetDatabase.GenerateUniqueAssetPath(this.path));

                    EditorGUIUtility.PingObject(instance);
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
            SerializedObject serializedObject, SerializedProperty serializedProperty, string directoryKey, string defaultDirectory, string defaultFileName)
            : base(serializedObject, serializedProperty, typeof(T), directoryKey, defaultDirectory, defaultFileName)
        {
        }

        protected virtual void Initialize(T instance)
        {
        }

        protected sealed override void Initialize(object obj)
        {
            this.Initialize((T)obj);
        }
    }
}
