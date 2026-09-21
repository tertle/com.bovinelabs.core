namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Extensions;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.Assertions;
    using UnityEngine.UIElements;

    public abstract class PrefabElementEditor : ElementEditor
    {
        private SerializedObject _prefabObject;

        protected virtual bool AllowChangesIfNoPrefab => true;

        private bool IsPrefab => ((Component)target).IsPrefab();

        protected override bool PreElementCreation(VisualElement root)
        {
            if (IsPrefab)
            {
                return true;
            }

            var prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(target);
            if (string.IsNullOrEmpty(prefabPath))
            {
                return AllowChangesIfNoPrefab;
            }

            var prefab = AssetDatabase.LoadAssetAtPath(prefabPath, target.GetType());
            if (prefab == null)
            {
                return AllowChangesIfNoPrefab;
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
                return CreatePropertyField(property);
            }

            var prefabProperty = _prefabObject.FindProperty(property.propertyPath);
            Assert.IsNotNull(prefabProperty);

            return CreatePropertyField(prefabProperty, _prefabObject);
        }

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
