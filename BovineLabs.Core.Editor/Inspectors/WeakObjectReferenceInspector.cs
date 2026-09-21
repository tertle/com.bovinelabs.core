namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Editor.Extensions;
    using BovineLabs.Core.Editor.Internal;
    using JetBrains.Annotations;
    using Unity.Entities.Content;
    using Unity.Entities.UI;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    internal abstract class WeakObjectReferenceInspector<T> : PropertyInspector<WeakObjectReference<T>>
        where T : Object
    {
        private Foldout _field;
        private ObjectField _objectField;

        public override VisualElement Build()
        {
            _field = new Foldout { value = false };

            _objectField = new ObjectField { enabledSelf = !IsReadOnly };
            InspectorUtility.AddRuntimeBar(_objectField);

            _field.Add(_objectField);

            Update();

            _objectField.RegisterValueChangedCallback(evt =>
            {
                Target = new WeakObjectReference<T>((T)evt.newValue);
            });

            return _field;
        }

        public override void Update()
        {
            var target = Target;

            var asset = target.GetEditorObject();
            _objectField!.value = asset;
            _field!.text = asset == null ? DisplayName : $"{DisplayName} : {asset.name}";
        }
    }

    [UsedImplicitly]
    internal class GameObjectWeakObjectReferenceInspector : WeakObjectReferenceInspector<GameObject>
    {
    }

    [UsedImplicitly]
    internal class TransformWeakObjectReferenceInspector : WeakObjectReferenceInspector<Transform>
    {
    }

    [UsedImplicitly]
    internal class MaterialWeakObjectReferenceInspector : WeakObjectReferenceInspector<Material>
    {
    }

    [UsedImplicitly]
    internal class MeshWeakObjectReferenceInspector : WeakObjectReferenceInspector<Mesh>
    {
    }

    [UsedImplicitly]
    internal class Texture2DArrayWeakObjectReferenceInspector : WeakObjectReferenceInspector<Texture2DArray>
    {
    }
}
