// <copyright file="IConvert.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Convert
{
    using Unity.Entities;

    public interface IConvert
    {
        void AddBlobAsset<T>(ref BlobAssetReference<T> blobAssetReference, out Hash128 objectHash)
            where T : unmanaged;

        void AddComponent<T>()
            where T : unmanaged, IComponentData;

        void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData;

        DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData;
    }
}
