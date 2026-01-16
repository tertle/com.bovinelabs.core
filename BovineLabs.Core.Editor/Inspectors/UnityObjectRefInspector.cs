// <copyright file="UnityObjectRefInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#pragma warning disable SA1402

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
        private ObjectField? objectField;

        /// <inheritdoc/>
        public override VisualElement Build()
        {
            this.objectField = new ObjectField { enabledSelf = !this.IsReadOnly };
            InspectorUtility.AddRuntimeBar(this.objectField);

            this.Update();

            this.objectField.RegisterValueChangedCallback(evt =>
            {
                this.Target = (T)evt.newValue;
            });

            return this.objectField;
        }

        /// <inheritdoc/>
        public override void Update()
        {
            var target = this.Target;

            this.objectField!.value = target.Value;
            this.objectField!.label = target.Value == null ? this.DisplayName : $"{this.DisplayName} : {target.Value.name}";
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

#if UNITY_INPUT
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
