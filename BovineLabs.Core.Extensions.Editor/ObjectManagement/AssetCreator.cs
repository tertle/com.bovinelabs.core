// <copyright file="AssetCreator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Editor.ObjectManagement
{
    using System.IO;
    using System.Linq;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.ObjectManagement;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public abstract class AssetCreator<T>
        where T : ScriptableObject, IUID
    {
        private readonly string path;
        private readonly SerializedObject serializedObject;
        private readonly SerializedProperty serializedProperty;
        private ListView? listView;

        protected AssetCreator(
            SerializedObject serializedObject,
            SerializedProperty serializedProperty,
            string directoryKey,
            string defaultDirectory,
            string defaultFileName)
        {
            this.serializedObject = serializedObject;
            this.serializedProperty = serializedProperty;

            this.Element = new PropertyField(serializedProperty);
            this.Element.Bind(this.serializedObject);
            this.Element.RegisterCallback<GeometryChangedEvent>(this.Init);

            this.Element.AddManipulator(new ContextualMenuManipulator(this.MenuBuilder));

            var directory = EditorSettingsUtility.GetAssetDirectory<T>(directoryKey, defaultDirectory);
            this.path = Path.Combine(directory, defaultFileName);

            this.serializedProperty.isExpanded = false;
        }

        public PropertyField Element { get; }

        protected virtual void Initialize(T instance)
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

            // this.listView.RegisterCallback<ChangeEvent<string>>(this.Callback);

            var removeButton = this.listView.Q<Button>("unity-list-view__remove-button");
            removeButton.parent.Remove(removeButton);

            this.listView.showBoundCollectionSize = false;

            this.listView.itemsAdded += ints =>
            {
                var count = ints.Count();
                for (var i = 0; i < count; i++)
                {
                    var instance = ScriptableObject.CreateInstance<T>();
                    this.Initialize(instance);
                    AssetDatabase.CreateAsset(instance, AssetDatabase.GenerateUniqueAssetPath(this.path));

                    EditorGUIUtility.PingObject(instance);
                }
            };

            this.listView.Q<VisualElement>("unity-content-container").SetEnabled(false);
        }

        private void Callback(ChangeEvent<string> evt)
        {
            this.listView!.Query<PropertyField>().ForEach(f =>
            {
                Debug.Log("Set");
                f.SetEnabled(false);
                f.MarkDirtyRepaint();
            });
            this.listView!.MarkDirtyRepaint();

            // if (this.initialized)
            // {
            //     listView.schedule.Execute(() =>
            //     {
            //
            //     }).ExecuteLater(100);
            //     return;
            // }
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
}
#endif
