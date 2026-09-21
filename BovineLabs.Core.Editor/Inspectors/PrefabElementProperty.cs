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
        private SerializedObject _prefabObject;

        private bool IsPrefab => ((Component)SerializedObject.targetObject).IsPrefab();

        protected override bool PreElementCreation(VisualElement root)
        {
            if (IsPrefab)
            {
                return true;
            }

            var target = SerializedObject.targetObject;

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

            _prefabObject = new SerializedObject(prefab);

            var label = new Label("Changes are applied to the prefab");
            ElementUtility.AddLabelStyles(label);
            root.Add(label);

            return true;
        }

        protected override VisualElement CreateElement(SerializedProperty property)
        {
            if (IsPrefab || _prefabObject == null)
            {
                return CreatePropertyField(property, SerializedObject);
            }

            var prefabProperty = _prefabObject.FindProperty(property.propertyPath);
            Assert.IsNotNull(prefabProperty);

            return CreatePropertyField(prefabProperty, _prefabObject);
        }
    }
}
