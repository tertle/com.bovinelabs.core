// <copyright file="BitArray256Converter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using BovineLabs.Core.Collections;
    using JetBrains.Annotations;
    using UnityEditor.UIElements;

    [UsedImplicitly]
    public class BitArray256Converter : UxmlAttributeConverter<BitArray256>
    {
        public override string ToString(BitArray256 value)
        {
            return $"{value.Data1},{value.Data2},{value.Data3},{value.Data4}";
        }

        public override BitArray256 FromString(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new BitArray256();
            }

            var items = value.Split(',');
            return items.Length != 4
                ? new BitArray256()
                : new BitArray256(ulong.Parse(items[0]), ulong.Parse(items[1]), ulong.Parse(items[2]), ulong.Parse(items[3]));
        }
    }
}
