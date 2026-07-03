// <copyright file="DynamicHashMapDenseWriteView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Collections.LowLevel.Unsafe;

    internal unsafe struct DynamicHashMapDenseWriteView<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        [NativeDisableUnsafePtrRestriction]
        internal DynamicHashMapHelper<TKey>* Data;

        internal int Count;
        internal int TotalSize;

        internal TKey* Keys => this.Data->Keys;

        internal byte* Values => this.Data->Values;
    }
}
