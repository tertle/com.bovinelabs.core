namespace BovineLabs.Core.Collections
{
    using System;

    public struct BlobMultiHashMapIterator<TKey>
        where TKey : struct, IEquatable<TKey>
    {
        internal TKey Key;
        internal int NextIndex;
    }
}
