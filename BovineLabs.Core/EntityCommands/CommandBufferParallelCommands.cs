// <copyright file="CommandBufferParallelCommands.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.EntityCommands
{
    using System;
    using BovineLabs.Core.Assertions;
    using Unity.Entities;

    public struct CommandBufferParallelCommands : IEntityCommands
    {
        private readonly int sortKey;
        private Entity localEntity;
        private EntityCommandBuffer.ParallelWriter commandBuffer;
        private BlobAssetStore blobAssetStore;

        public CommandBufferParallelCommands(
            EntityCommandBuffer.ParallelWriter commandBuffer,
            int sortKey,
            Entity localEntity = default,
            BlobAssetStore blobAssetStore = default)
        {
            this.commandBuffer = commandBuffer;
            this.sortKey = sortKey;
            this.localEntity = localEntity;
            this.blobAssetStore = blobAssetStore;
        }

        public Entity Entity
        {
            get => this.localEntity;
            set => this.localEntity = value;
        }

        public Entity CreateEntity()
        {
            this.localEntity = this.commandBuffer.CreateEntity(this.sortKey);
            return this.localEntity;
        }

        public Entity Instantiate(Entity prefab)
        {
            this.localEntity = this.commandBuffer.Instantiate(this.sortKey, prefab);
            return this.localEntity;
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
            this.AddComponent<T>(this.localEntity);
        }

        public void AddComponent<T>(Entity entity) where T : unmanaged, IComponentData
        {
            this.commandBuffer.AddComponent<T>(this.sortKey, entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.AddComponent(this.localEntity, component);
        }

        public void AddComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            this.commandBuffer.AddComponent(this.sortKey, entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            this.AddComponent(this.localEntity, components);
        }

        public void AddComponent(Entity entity, in ComponentTypeSet components)
        {
            this.commandBuffer.AddComponent(this.sortKey, entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.SetComponent(this.localEntity, component);
        }

        public void SetComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            this.commandBuffer.SetComponent(this.sortKey, entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.AddBuffer<T>(this.localEntity);
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return this.commandBuffer.AddBuffer<T>(this.sortKey, entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.SetBuffer<T>(this.localEntity);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return this.commandBuffer.SetBuffer<T>(this.sortKey, entity);
        }

        public void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData
        {
            this.AppendToBuffer(this.localEntity, element);
        }

        public void AppendToBuffer<T>(Entity entity, in T element)
            where T : unmanaged, IBufferElementData
        {
            this.commandBuffer.AppendToBuffer(this.sortKey, entity, element);
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.SetComponentEnabled<T>(this.localEntity, enabled);
        }

        public void SetComponentEnabled<T>(Entity entity, bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.commandBuffer.SetComponentEnabled<T>(this.sortKey, entity, enabled);
        }
    }
}
