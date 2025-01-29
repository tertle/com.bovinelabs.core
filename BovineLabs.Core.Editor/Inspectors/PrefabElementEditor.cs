// <copyright file="PrefabElementEditor.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Extensions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.UIElements;

    /// <summary> A custom editor that will cause you to edit the source prefab instead of instances. </summary>
    public abstract class PrefabElementEditor : ElementEditor
    {
        private SerializedObject? prefabObject;

        protected virtual bool AllowChangesIfNoPrefab => true;

        private bool IsPrefab => ((Component)this.target).IsPrefab();

        protected override bool PreElementCreation(VisualElement root)
        {
            if (this.IsPrefab)
            {
                return true;
            }

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(this.target);
            if (string.IsNullOrEmpty(prefabPath))
            {
                return this.AllowChangesIfNoPrefab;
            }

            var prefab = AssetDatabase.LoadAssetAtPath(prefabPath, this.target.GetType());
            if (prefab == null)
            {
                return this.AllowChangesIfNoPrefab;
            }

            this.prefabObject = new SerializedObject(prefab);

            var label = new Label("Changes are applied to the prefab");
            ElementUtility.AddLabelStyles(label);
            root.Add(label);

            return true;
        }

        /// <inheritdoc />
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            if (this.IsPrefab || this.prefabObject == null)
            {
                return CreatePropertyField(property);
            }

            var prefabProperty = this.prefabObject.FindProperty(property.propertyPath);
            Assert.IsNotNull(prefabProperty);

            return CreatePropertyField(prefabProperty, this.prefabObject);
        }

        /// <inheritdoc />
        protected override void PostElementCreation(VisualElement root, bool createdElements)
        {
            if (createdElements)
            {
                return;
            }

            var label = new Label("Can only apply changes if it's a prefab");
            ElementUtility.AddLabelStyles(label);
            root.Add(label);
        }
    }
}
