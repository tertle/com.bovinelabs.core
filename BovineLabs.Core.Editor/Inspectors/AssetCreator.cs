// <copyright file="AssetCreator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System.Collections.Generic;
    using System.IO;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public abstract class AssetCreator<T>
        where T : ScriptableObject, IKeyedAsset
    {
        private readonly string path;
        private readonly SerializedObject serializedObject;

        protected AssetCreator(
            SerializedObject serializedObject,
            SerializedProperty serializedProperty,
            string directoryKey,
            string defaultDirectory,
            string defaultFileName)
        {
            this.serializedObject = serializedObject;

            this.Element = new PropertyField(serializedProperty);
            this.Element.Bind(this.serializedObject);
            this.Element.RegisterCallback<GeometryChangedEvent>(this.Init);

            var directory = EditorSettingsUtility.GetAssetDirectory<T>(directoryKey, defaultDirectory);
            this.path = Path.Combine(directory, defaultFileName);
        }

        public PropertyField Element { get; }

        protected virtual void Initialize(T instance, SerializedProperty property)
        {
        }

        private static int GetFirstFreeKey(SerializedProperty property)
        {
            var keys = new List<int>();

            for (var i = 0; i < property.arraySize; i++)
            {
                var element = property.GetArrayElementAtIndex(i);
                if (element.objectReferenceValue == null)
                {
                    continue;
                }

                var value = (T)element.objectReferenceValue;
                keys.Add(value.Key);
            }

            keys.Sort();

            var index = 0;
            for (var i = 0; i < keys.Count; i++)
            {
                // Key already exists, go next
                if (keys[i] == i)
                {
                    index = i + 1;
                }
                else
                {
                    // Empty slot use this
                    break;
                }
            }

            return index;
        }

        private void Init(GeometryChangedEvent evt)
        {
            var listView = this.Element.Q<ListView>();
            if (listView == null)
            {
                return;
            }

            this.Element.UnregisterCallback<GeometryChangedEvent>(this.Init);

            var removeButton = listView.Q<Button>("unity-list-view__remove-button");
            removeButton.parent.Remove(removeButton);

            listView.showBoundCollectionSize = false;

            listView.itemsAdded += ints =>
            {
                this.serializedObject.Update();
                var serializedProperty = this.serializedObject.FindProperty(this.Element.bindingPath);

                foreach (var index in ints)
                {
                    var instance = ScriptableObject.CreateInstance<T>();
                    var key = GetFirstFreeKey(serializedProperty);
                    instance.Key = key;
                    this.Initialize(instance, serializedProperty);

                    AssetDatabase.CreateAsset(instance, AssetDatabase.GenerateUniqueAssetPath(this.path));

                    serializedProperty.GetArrayElementAtIndex(index).objectReferenceValue = instance;
                }

                this.serializedObject.ApplyModifiedProperties();
            };
        }
    }
}
