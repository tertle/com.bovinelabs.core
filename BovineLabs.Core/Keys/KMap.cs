// <copyright file="KMap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine;

    internal struct KMap
    {
        internal const int MaxCapacity = 255;

        private FixedList4096Bytes<MiniString> keys;
        private FixedList512Bytes<byte> values;
        private FixedList512Bytes<byte> next;
        private FixedList512Bytes<byte> buckets;

        public KMap(IReadOnlyList<NameValue> kvp)
        {
            if (kvp.Count > MaxCapacity)
            {
                throw new ArgumentException($"Container length {kvp.Count} exceeds max capacity {MaxCapacity}", nameof(kvp));
            }

            if (kvp.Distinct().Count() != kvp.Count)
            {
                Debug.LogError("Non unique keys found in key map, duplicates will be ignored.");
            }

            this.keys = default;
            this.values = default;
            this.next = default;
            this.buckets = default;

            this.keys.Length = MaxCapacity;
            this.values.Length = MaxCapacity;
            this.next.Length = MaxCapacity;
            this.buckets.Length = MaxCapacity;

            unsafe
            {
                // this resets buckets and next arrays to 255
                UnsafeUtility.MemSet(UnsafeUtility.AddressOf(ref this.buckets.ElementAt(0)), 0xff, this.buckets.Length * UnsafeUtility.SizeOf<byte>());
                UnsafeUtility.MemSet(UnsafeUtility.AddressOf(ref this.next.ElementAt(0)), 0xff, this.next.Length * UnsafeUtility.SizeOf<byte>());
            }

            for (byte index = 0; index < kvp.Count; index++)
            {
                var key = (MiniString)kvp[index].Name;
                var value = kvp[index].Value.Value;
                var bucket = GetBucket(key);

                this.keys[index] = key;
                this.values[index] = value;
                this.next[index] = this.buckets[bucket];
                this.buckets[bucket] = index;
            }
        }

        public bool TryGetValue(MiniString key, out byte value)
        {
            var bucket = GetBucket(key);
            var index = this.buckets[bucket];

            if (index == 255)
            {
                value = default;
                return false;
            }

            while (!this.keys[index].Equals(key))
            {
                index = this.next[index];
                if (index == 255)
                {
                    value = default;
                    return false;
                }
            }

            value = this.values[index];
            return true;
        }

        // This will bias 0 slightly more commonly as we remap 255 to 0 to use 255 as a does not exist check
        private static int GetBucket(MiniString key)
        {
            var bucket = key.GetHashCode() & MaxCapacity;
            bucket = math.select(bucket, 0, bucket == 255);
            return bucket;
        }
    }
}
