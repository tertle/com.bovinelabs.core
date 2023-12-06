// <copyright file="KMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Collections;

    public struct KMap
    {
        public const int MaxCapacity = 256;

        private readonly FixedHashMap<MiniString, int, Data> map;
        private readonly FixedHashMap<int, MiniString, Data> reverse;

        internal KMap(IReadOnlyList<NameValue> kvp)
        {
            if (kvp.Count > MaxCapacity)
            {
                throw new ArgumentException($"Container length {kvp.Count} exceeds max capacity {MaxCapacity}", nameof(kvp));
            }

            this.map = new FixedHashMap<MiniString, int, Data>(default);
            this.reverse = new FixedHashMap<int, MiniString, Data>(default);

            Check.Assume(this.map.Capacity >= MaxCapacity);

            for (var index = 0; index < kvp.Count; index++)
            {
                var key = (MiniString)kvp[index].Name;

                this.map.TryAdd(key, kvp[index].Value);
                this.reverse.TryAdd(kvp[index].Value, key);
            }
        }

        internal int Capacity => this.map.Capacity;

        internal bool TryGetValue(MiniString key, out int value)
        {
            return this.map.TryGetValue(key, out value);
        }

        internal bool TryGetValue(int value, out MiniString key)
        {
            return this.reverse.TryGetValue(value, out key);
        }

        [StructLayout(LayoutKind.Explicit, Size = 8192)]
        private struct Data
        {
        }
    }
}
