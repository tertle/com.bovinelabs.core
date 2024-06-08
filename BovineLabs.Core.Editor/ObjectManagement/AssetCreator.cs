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
        private ListView? listView;

        public AssetCreator(SerializedObject serializedObject, SerializedProperty serializedProperty, Type type)
        {
            this.serializedObject = serializedObject;
            this.serializedProperty = serializedProperty;
            this.type = type;
            this.serializedProperty.isExpanded = false;

            this.Element = PropertyUtil.CreateProperty(serializedProperty, this.serializedObject);

            if (!TryGetDirectory(this.type, out this.path))
            {
                return;
            }

            this.Element.RegisterCallback<GeometryChangedEvent>(this.Init);
            this.Element.AddManipulator(new ContextualMenuManipulator(this.MenuBuilder));
        }

        public PropertyField Element { get; }

        public static bool TryGetDirectory(Type type, out string path)
        {
            var assetCreatorAttribute = type.GetCustomAttribute<AssetCreatorAttribute>();
            if (assetCreatorAttribute == null)
            {
                path = string.Empty;
                Debug.LogError($"Type {type} does not have {nameof(AssetCreatorAttribute)} but is being created via AssetCreator");
                return false;
            }

            var directory = EditorSettingsUtility.GetAssetDirectory(assetCreatorAttribute.DirectoryKey, assetCreatorAttribute.DefaultDirectory);
            path = Path.Combine(directory, assetCreatorAttribute.DefaultFileName);
            return true;
        }

        public static bool TryGetDirectory(Type type, string defaultFileName, out string path)
        {
            var assetCreatorAttribute = type.GetCustomAttribute<AssetCreatorAttribute>();
            if (assetCreatorAttribute == null)
            {
                path = string.Empty;
                Debug.LogError($"Type {type} does not have {nameof(AssetCreatorAttribute)} but is being created via AssetCreator");
                return false;
            }

            var directory = EditorSettingsUtility.GetAssetDirectory(assetCreatorAttribute.DirectoryKey, assetCreatorAttribute.DefaultDirectory);
            path = Path.Combine(directory, defaultFileName);
            return true;
        }

        protected virtual void Initialize(object instance)
        {
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
        where T : ScriptableObject, IUID
    {
        public AssetCreator(SerializedObject serializedObject, SerializedProperty serializedProperty)
            : base(serializedObject, serializedProperty, typeof(T))
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