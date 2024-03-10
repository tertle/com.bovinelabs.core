// <copyright file="UnityObjectRefInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using JetBrains.Annotations;
    using Unity.Entities;
    using Unity.Entities.UI;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;
    using Object = UnityEngine.Object;

    internal class UnityObjectRefInspector<T> : PropertyInspector<UnityObjectRef<T>>
        where T : Object
    {
        public override VisualElement Build()
        {
            var value = this.Target.Value;
            var name = value == null ? this.DisplayName : $"{this.DisplayName} : {value.name}";
            var field = new Foldout { text = name, value = false };

            var id = new IntegerField("Instance Id") { value = this.Target.Id.instanceId };
            id.SetEnabled(false);

            var of = new ObjectField { value = this.Target.Value };
            of.SetEnabled(false);

            field.Add(id);
            field.Add(of);

            return field;
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
}
