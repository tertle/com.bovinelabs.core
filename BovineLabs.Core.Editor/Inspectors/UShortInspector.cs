// <copyright file="UShortInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using JetBrains.Annotations;
    using Unity.Mathematics;
    using Unity.Properties;
    using UnityEditor.UIElements;

    [UsedImplicitly]
    internal class UShortInspector : BaseFieldInspector<IntegerField, int, ushort>
    {
        static UShortInspector()
        {
            TypeConversion.Register<ushort, int>(v => v);
            TypeConversion.Register<int, ushort>(v => (ushort)math.clamp(v, ushort.MinValue, ushort.MaxValue));
        }
    }

    [UsedImplicitly]
    internal class ShortInspector : BaseFieldInspector<IntegerField, int, short>
    {
        static ShortInspector()
        {
            TypeConversion.Register<short, int>(v => v);
            TypeConversion.Register<int, short>(v => (short)math.clamp(v, short.MinValue, short.MaxValue));
        }
    }
}
