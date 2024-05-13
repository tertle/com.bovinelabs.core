// <copyright file="UnsafeHashMapBucketData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;

    public unsafe struct UnsafeHashMapBucketData<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        public readonly TValue* Values;

        public readonly TKey* Keys;

        public readonly int* Next;

        public readonly int* Buckets;

        internal UnsafeHashMapBucketData(TValue* v, TKey* k, int* n, int* b)
        {
            this.Values = v;
            this.Keys = k;
            this.Next = n;
            this.Buckets = b;
        }
    }
}
