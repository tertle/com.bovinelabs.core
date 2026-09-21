
namespace BovineLabs.Core.Editor.Inspectors
{
    using BovineLabs.Core.Editor.Internal;
    using JetBrains.Annotations;
    using Unity.Entities;
    using Unity.Entities.UI;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    internal abstract class UnityObjectRefInspector<T> : PropertyInspector<UnityObjectRef<T>>
        where T : Object
    {
        private ObjectField _objectField;

        public override VisualElement Build()
        {
            _objectField = new ObjectField { enabledSelf = !IsReadOnly };
            InspectorUtility.AddRuntimeBar(_objectField);

            Update();

            _objectField.RegisterValueChangedCallback(evt =>
            {
                Target = (T)evt.newValue;
            });

            return _objectField;
        }

        public override void Update()
        {
            var target = Target;

            _objectField!.value = target.Value;
            _objectField!.label = target.Value == null ? DisplayName : $"{DisplayName} : {target.Value.name}";
        }
    }

    [UsedImplicitly]
    internal class GameObjectUnityObjectRefInspector : UnityObjectRefInspector<GameObject>
    {
    }

    [UsedImplicitly]
    internal class TransformUnityObjectRefInspector : UnityObjectRefInspector<Transform>
    {
    }

    [UsedImplicitly]
    internal class MaterialUnityObjectRefInspector : UnityObjectRefInspector<Material>
    {
    }

    [UsedImplicitly]
    internal class MeshUnityObjectRefInspector : UnityObjectRefInspector<Mesh>
    {
    }

    [UsedImplicitly]
    internal class Texture2DArrayUnityObjectRefInspector : UnityObjectRefInspector<Texture2DArray>
    {
    }

    [UsedImplicitly]
    internal class AudioClipUnityObjectRefInspector : UnityObjectRefInspector<AudioClip>
    {
    }

#if UNITY_INPUT_SYSTEM
    [UsedImplicitly]
    internal class InputActionAssetUnityObjectRefInspector : UnityObjectRefInspector<UnityEngine.InputSystem.InputActionAsset>
    {
    }

    [UsedImplicitly]
    internal class InputActionReferenceUnityObjectRefInspector : UnityObjectRefInspector<UnityEngine.InputSystem.InputActionReference>
    {
    }
#endif

#if UNITY_SPLINES
    [UsedImplicitly]
    internal class SplineContainerUnityObjectRefInspector : UnityObjectRefInspector<UnityEngine.Splines.SplineContainer>
    {
    }
#endif
}
