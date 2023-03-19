// <copyright file="CommandBufferParallelConvert.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Convert
{
    using Unity.Entities;

    public struct CommandBufferParallelConvert : IConvert
    {
        private readonly Entity entity;
        private readonly int sortKey;
        private EntityCommandBuffer.ParallelWriter commandBuffer;
        private BlobAssetStore blobAssetStore;

        public CommandBufferParallelConvert(
            EntityCommandBuffer.ParallelWriter commandBuffer,
            int sortKey,
            Entity entity,
            BlobAssetStore blobAssetStore = default)
        {
            this.commandBuffer = commandBuffer;
            this.sortKey = sortKey;
            this.entity = entity;
            this.blobAssetStore = blobAssetStore;
        }

        public CommandBufferParallelConvert(EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, BlobAssetStore blobAssetStore = default)
        {
            this.commandBuffer = commandBuffer;
            this.sortKey = sortKey;
            this.blobAssetStore = blobAssetStore;

            this.entity = commandBuffer.CreateEntity(sortKey);
        }

        public void AddBlobAsset<T>(ref BlobAssetReference<T> blobAssetReference, out Hash128 objectHash)
            where T : unmanaged
        {
            if (this.blobAssetStore.IsCreated)
            {
                this.blobAssetStore.TryAdd(ref blobAssetReference, out objectHash);
            }
            else
            {
                objectHash = default;
            }
        }

        public void AddComponent<T>()
            where T : unmanaged, IComponentData
        {
            this.commandBuffer.AddComponent<T>(this.sortKey, this.entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.commandBuffer.AddComponent(this.sortKey, this.entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            this.commandBuffer.AddComponent(this.sortKey, this.entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.commandBuffer.SetComponent(this.sortKey, this.entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.commandBuffer.AddBuffer<T>(this.sortKey, this.entity);
        }
    }
}
