// <copyright file="HalfInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using JetBrains.Annotations;
    using Unity.Mathematics;
    using Unity.Properties;
    using UnityEditor.UIElements;

    [UsedImplicitly]
    internal class HalfInspector : BaseFieldInspector<FloatField, float, half>
    {
        static HalfInspector()
        {
            TypeConversion.Register<half, float>(v => v);
            TypeConversion.Register<float, half>(v => new half(v));
        }
    }
}
