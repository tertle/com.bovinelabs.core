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
