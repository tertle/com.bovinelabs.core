// <copyright file="DynamicHashMapTraversalOrder.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    internal enum DynamicHashMapTraversalOrder : byte
    {
        DenseIndex,
        BucketChain,
    }
}
