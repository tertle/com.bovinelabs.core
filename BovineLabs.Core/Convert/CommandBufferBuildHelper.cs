// <copyright file="CommandBufferBuildHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Convert
{
    using Unity.Entities;

    public struct CommandBufferBuildHelper : IConvert
    {
        private readonly Entity entity;
        private EntityCommandBuffer commandBuffer;
        private BlobAssetStore blobAssetStore;

        public CommandBufferBuildHelper(EntityCommandBuffer commandBuffer, Entity entity, BlobAssetStore blobAssetStore = default)
        {
            this.commandBuffer = commandBuffer;
            this.entity = entity;
            this.blobAssetStore = blobAssetStore;
        }

        public CommandBufferBuildHelper(EntityCommandBuffer commandBuffer, BlobAssetStore blobAssetStore = default)
        {
            this.commandBuffer = commandBuffer;
            this.blobAssetStore = blobAssetStore;

            this.entity = commandBuffer.CreateEntity();
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
            this.commandBuffer.AddComponent<T>(this.entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.commandBuffer.AddComponent(this.entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            this.commandBuffer.AddComponent(this.entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.commandBuffer.SetComponent(this.entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.commandBuffer.AddBuffer<T>(this.entity);
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.commandBuffer.SetComponentEnabled<T>(this.entity, enabled);
        }
    }
}
