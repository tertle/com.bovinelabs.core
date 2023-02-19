// <copyright file="HalfInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using JetBrains.Annotations;
    using Unity.Mathematics;
    using Unity.Properties;
    using UnityEngine;
    using UnityEngine.UIElements;

    [UsedImplicitly]
    internal class HalfInspector : BaseFieldInspector<FloatField, float, half>
    {
        static HalfInspector()
        {
            TypeConversion.Register((ref half v) => (float)v);
            TypeConversion.Register((ref float v) => new half(v));
        }
    }

    [UsedImplicitly]
    internal class Half2Inspector : BaseFieldInspector<Vector2Field, Vector2, half2>
    {
        static Half2Inspector()
        {
            TypeConversion.Register((ref half2 v) => (Vector2)(float2)v);
            TypeConversion.Register((ref Vector2 v) => (half2)(float2)v);
        }
    }

    [UsedImplicitly]
    internal class Half3Inspector : BaseFieldInspector<Vector3Field, Vector3, half3>
    {
        static Half3Inspector()
        {
            TypeConversion.Register((ref half3 v) => (Vector3)(float3)v);
            TypeConversion.Register((ref Vector3 v) => (half3)(float3)v);
        }
    }

    [UsedImplicitly]
    internal class Half4Inspector : BaseFieldInspector<Vector4Field, Vector4, half4>
    {
        static Half4Inspector()
        {
            TypeConversion.Register((ref half4 v) => (Vector4)(float4)v);
            TypeConversion.Register((ref Vector4 v) => (half4)(float4)v);
        }
    }
}
