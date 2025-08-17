// <copyright file="WeakObjectReferenceInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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
        private Foldout? field;
        private ObjectField? objectField;

        /// <inheritdoc/>
        public override VisualElement Build()
        {
            this.field = new Foldout { value = false };

            this.objectField = new ObjectField { enabledSelf = !this.IsReadOnly };
            InspectorUtility.AddRuntimeBar(this.objectField);

            this.field.Add(this.objectField);

            this.Update();

            this.objectField.RegisterValueChangedCallback(evt =>
            {
                this.Target = new WeakObjectReference<T>((T)evt.newValue);
            });

            return this.field;
        }

        /// <inheritdoc/>
        public override void Update()
        {
            var target = this.Target;

            var asset = target.GetEditorObject();
            this.objectField!.value = asset;
            this.field!.text = asset == null ? this.DisplayName : $"{this.DisplayName} : {asset.name}";
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
