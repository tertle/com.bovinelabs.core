// <copyright file="UShortInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using JetBrains.Annotations;
    using Unity.Mathematics;
    using Unity.Properties;


    [UsedImplicitly]
    internal class UShortInspector : BaseFieldInspector<UnityEngine.UIElements.IntegerField, int, ushort>
    {
        static UShortInspector()
        {
            TypeConversion.Register((ref ushort v) => (int)v);
            TypeConversion.Register((ref int v) => (ushort)math.clamp(v, ushort.MinValue, ushort.MaxValue));
        }
    }

    [UsedImplicitly]
    internal class ShortInspector : BaseFieldInspector<UnityEngine.UIElements.IntegerField, int, short>
    {
        static ShortInspector()
        {
            TypeConversion.Register((ref short v) => (int)v);
            TypeConversion.Register((ref int v) => (short)math.clamp(v, short.MinValue, short.MaxValue));
        }
    }
}
