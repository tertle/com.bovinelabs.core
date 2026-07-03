// <copyright file="DynamicHashCollectionPolicy.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    internal interface IDynamicHashCollectionPolicy
    {
        DynamicHashMapTraversalOrder TraversalOrder { get; }

        DynamicHashMapDuplicatePolicy DuplicatePolicy { get; }

        DynamicHashMapRebuildOrder RebuildOrder { get; }

        ulong CollectionSchemaHash { get; }
    }

    internal readonly struct UniqueHashMapPolicy : IDynamicHashCollectionPolicy
    {
        public DynamicHashMapTraversalOrder TraversalOrder => DynamicHashMapTraversalOrder.DenseIndex;

        public DynamicHashMapDuplicatePolicy DuplicatePolicy => DynamicHashMapDuplicatePolicy.RejectDuplicateKeys;

        public DynamicHashMapRebuildOrder RebuildOrder => DynamicHashMapRebuildOrder.Default;

        public ulong CollectionSchemaHash => DynamicGhostPrimitiveCodec.Hash64("CollectionKind:DynamicHashMap");
    }

    internal readonly struct MultiHashMapPolicy : IDynamicHashCollectionPolicy
    {
        public DynamicHashMapTraversalOrder TraversalOrder => DynamicHashMapTraversalOrder.BucketChain;

        public DynamicHashMapDuplicatePolicy DuplicatePolicy => DynamicHashMapDuplicatePolicy.AllowDuplicateKeys;

        public DynamicHashMapRebuildOrder RebuildOrder => DynamicHashMapRebuildOrder.PreservePackedChainOrder;

        public ulong CollectionSchemaHash
        {
            get
            {
                var hash = DynamicGhostPrimitiveCodec.Hash64("CollectionKind:DynamicMultiHashMap");
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("TraversalOrder:BucketChain"));
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("DuplicatePolicy:AllowDuplicateKeys"));
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("RebuildOrder:PreservePackedChainOrder"));
                hash = DynamicGhostPrimitiveCodec.CombineHash64(hash, DynamicGhostPrimitiveCodec.Hash64("PreserveValueOrder:True"));
                return hash;
            }
        }
    }
}
#endif
