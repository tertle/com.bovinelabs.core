// <copyright file="HalfInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using JetBrains.Annotations;
    using Unity.Mathematics;
    using Unity.Properties;


    [UsedImplicitly]
    internal class HalfInspector : BaseFieldInspector<UnityEngine.UIElements.FloatField, float, half>
    {
        static HalfInspector()
        {
            TypeConversion.Register((ref half v) => (float)v);
            TypeConversion.Register((ref float v) => new half(v));
        }
    }
}
