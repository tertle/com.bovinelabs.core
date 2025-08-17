// <copyright file="PrefabElementProperty.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.PropertyDrawers;
    using Unity.Assertions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    [CustomPropertyDrawer(typeof(PrefabElementAttribute))]
    public class PrefabElementProperty : ElementProperty
    {
        private SerializedObject? prefabObject;

        private bool IsPrefab => ((Component)this.SerializedObject.targetObject).IsPrefab();

        /// <inheritdoc/>
        protected override bool PreElementCreation(VisualElement root)
        {
            if (this.IsPrefab)
            {
                return true;
            }

            var target = this.SerializedObject.targetObject;

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            if (string.IsNullOrEmpty(prefabPath))
            {
                return true;
            }

            var prefab = AssetDatabase.LoadAssetAtPath(prefabPath, target.GetType());
            if (prefab == null)
            {
                return true;
            }

            this.prefabObject = new SerializedObject(prefab);

            var label = new Label("Changes are applied to the prefab");
            ElementUtility.AddLabelStyles(label);
            root.Add(label);

            return true;
        }

        /// <inheritdoc/>
        protected override VisualElement CreateElement(SerializedProperty property)
        {
            if (this.IsPrefab || this.prefabObject == null)
            {
                return CreatePropertyField(property, this.SerializedObject);
            }

            var prefabProperty = this.prefabObject.FindProperty(property.propertyPath);
            Assert.IsNotNull(prefabProperty);

            return CreatePropertyField(prefabProperty, this.prefabObject);
        }
    }
}
