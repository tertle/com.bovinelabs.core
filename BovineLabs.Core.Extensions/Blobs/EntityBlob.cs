// <copyright file="EntityBlob.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Blobs
{
    using BovineLabs.Core.Collections;
    using JetBrains.Annotations;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Properties;

    public unsafe struct EntityBlob : IComponentData
    {
        /// <summary> Key, BlobPtr{BlobAssetHeader} pairs but BlobPtr isn't equitable so we just use int instead and reinterpret </summary>
        internal BlobAssetReference<BlobPerfectHashMap<int, int>> Value;

        [UsedImplicitly]
        [CreateProperty(ReadOnly = true)]
        private int BlobCount => this.Value.IsCreated ? this.Value.Value.Capacity : 0;

        [UsedImplicitly]
        [CreateProperty(ReadOnly = true)]
        private int BlobSize => this.Value.IsCreated ? this.Value.m_data.Header->Length : 0;

        public bool TryGet<T>(int key, out BlobAssetReference<T> blobAssetReference)
            where T : unmanaged
        {
            if (!this.Value.Value.TryGetValue(key, out var offsetPtr))
            {
                blobAssetReference = default;
                return false;
            }

            ref var offset = ref UnsafeUtility.As<int, BlobPtr<BlobAssetHeader>>(ref offsetPtr.Ref);

            var header = (BlobAssetHeader*)offset.GetUnsafePtr();
            var blobPtr = (byte*)(header + 1);
            header->ValidationPtr = blobPtr;

            blobAssetReference = new BlobAssetReference<T> { m_data = new BlobAssetReferenceData { m_Ptr = blobPtr } };

            return true;
        }

        public BlobAssetReference<T> Get<T>(int key)
            where T : unmanaged
        {
            ref var offsetPtr = ref this.Value.Value[key];
            ref var offset = ref UnsafeUtility.As<int, BlobPtr<BlobAssetHeader>>(ref offsetPtr);

            var header = (BlobAssetHeader*)offset.GetUnsafePtr();
            var blobPtr = (byte*)(header + 1);
            header->ValidationPtr = blobPtr;

            return new BlobAssetReference<T> { m_data = new BlobAssetReferenceData { m_Ptr = blobPtr } };
        }
    }
}
