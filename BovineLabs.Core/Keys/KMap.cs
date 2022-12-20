// <copyright file="KMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;

    internal struct KMap
    {
        internal const int MaxCapacity = 255;

        private readonly FixedHashMap<MiniString, byte, Data> map;
        private readonly FixedHashMap<byte, MiniString, Data> reverse;

        public KMap(IReadOnlyList<NameValue> kvp)
        {
            if (kvp.Count > MaxCapacity)
            {
                throw new ArgumentException($"Container length {kvp.Count} exceeds max capacity {MaxCapacity}", nameof(kvp));
            }

            this.map = new FixedHashMap<MiniString, byte, Data>(default);
            this.reverse = new FixedHashMap<byte, MiniString, Data>(default);

            for (byte index = 0; index < kvp.Count; index++)
            {
                var key = (MiniString)kvp[index].Name;

                this.map.TryAdd(key, kvp[index].Value);
                this.reverse.TryAdd(kvp[index].Value, key);
            }
        }

        public bool TryGetValue(MiniString key, out byte value)
        {
            return this.map.TryGetValue(key, out value);
        }

        public bool TryGetValue(byte value, out MiniString key)
        {
            return this.reverse.TryGetValue(value, out key);
        }

        [StructLayout(LayoutKind.Explicit, Size = 8192)]
        private struct Data
        {
        }
    }
}
