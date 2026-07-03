// <copyright file="DynamicHashMapLayout.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    internal struct DynamicHashMapLayout
    {
        internal int ValuesOffset;
        internal int KeysOffset;
        internal int NextOffset;
        internal int BucketsOffset;
        internal int Capacity;
        internal int BucketCapacity;
        internal int SizeOfTValue;
        internal int TotalSize;
    }
}
