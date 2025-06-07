// <copyright file="UnityObjectRefInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private IntegerField? idField;
        private ObjectField? objectField;
        private Foldout? field;

        public override VisualElement Build()
        {
            this.field = new Foldout { value = false };

            this.idField = new IntegerField("Instance Id");
            this.idField.SetEnabled(false);
            InspectorUtility.AddRuntimeBar(this.idField);

            this.objectField = new ObjectField { enabledSelf = !this.IsReadOnly };
            InspectorUtility.AddRuntimeBar(this.objectField);

            this.field.Add(this.idField);
            this.field.Add(this.objectField);

            this.Update();

            this.objectField.RegisterValueChangedCallback(evt =>
            {
                this.Target = (T)evt.newValue;
            });

            return this.field;
        }

        public override void Update()
        {
            var target = this.Target;

            this.idField!.value = target.Id.instanceId;
            this.objectField!.value = target.Value;
            this.field!.text = target.Value == null ? this.DisplayName : $"{this.DisplayName} : {target.Value.name}";
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

#if !BL_DISABLE_INPUT
    [UsedImplicitly]
    internal class InputActionAssetUnityObjectRefInspector : UnityObjectRefInspector<UnityEngine.InputSystem.InputActionAsset>
    {
    }

    [UsedImplicitly]
    internal class InputActionReferenceUnityObjectRefInspector : UnityObjectRefInspector<UnityEngine.InputSystem.InputActionReference>
    {
    }
#endif
}
