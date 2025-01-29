// <copyright file="EntityBlobBakingBlobStoreSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.Blobs
{
    using BovineLabs.Core.Blobs;
    using Unity.Entities;

    /// <summary>
    /// The EntityBlobBakingBlobStoreSystem is responsible for storing and disposing the <see cref="BlobAssetReference{T}" /> created in systems nad stored on
    /// <see cref="EntityBlobBakedData" /> as well as writing the <see cref="EntityBlob" /> created in <see cref="EntityBlobBakingSystem" /> to the baking
    /// <see cref="BlobAssetStore" />.
    /// </summary>
    [UpdateAfter(typeof(EntityBlobBakingSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial class EntityBlobBakingBlobStoreSystem : SystemBase
    {
        private BlobAssetStore blobAssetStore;
        private BlobAssetStore localAssetStore;

        protected override void OnCreate()
        {
            this.blobAssetStore = this.World.GetExistingSystemManaged<BakingSystem>().BlobAssetStore;
            this.localAssetStore = new BlobAssetStore(16 * 1024);
        }

        protected override void OnDestroy()
        {
            this.localAssetStore.Dispose();
        }

        protected override void OnUpdate()
        {
            foreach (var blob in SystemAPI.Query<RefRW<EntityBlob>>())
            {
                this.blobAssetStore.TryAdd(ref blob.ValueRW.Value, out _);
            }

            foreach (var blob in SystemAPI.Query<RefRW<EntityBlobBakedData>>())
            {
                this.localAssetStore.TryAdd(ref blob.ValueRW.Blob, out _);
            }
        }
    }
}
