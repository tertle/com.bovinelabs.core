// <copyright file="BlobMultiHashMapIterator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;

    public struct BlobMultiHashMapIterator<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        internal TKey Key;
        internal int NextIndex;
    }
}
