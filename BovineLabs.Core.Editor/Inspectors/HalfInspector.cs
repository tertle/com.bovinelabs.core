// <copyright file="HalfInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using JetBrains.Annotations;
    using Unity.Mathematics;
    using Unity.Properties;
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
}
