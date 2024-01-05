// <copyright file="CommandBufferCommands.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.EntityCommands
{
    using System;
    using BovineLabs.Core.Assertions;
    using Unity.Burst;
    using Unity.Entities;

    public struct CommandBufferCommands : IEntityCommands
    {
        private Entity entity;
        private EntityCommandBuffer commandBuffer;
        private BlobAssetStore blobAssetStore;

        public CommandBufferCommands(EntityCommandBuffer commandBuffer, Entity entity = default, BlobAssetStore blobAssetStore = default)
        {
            this.commandBuffer = commandBuffer;
            this.entity = entity;
            this.blobAssetStore = blobAssetStore;
        }

        public Entity Entity
        {
            get => this.entity;
            set => this.entity = value;
        }

        public Entity CreateEntity()
        {
            this.entity = this.commandBuffer.CreateEntity();
            return this.entity;
        }

        public Entity Instantiate(Entity prefab)
        {
            this.entity = this.commandBuffer.Instantiate(prefab);
            return this.entity;
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
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.commandBuffer.AddComponent<T>(this.entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.commandBuffer.AddComponent(this.entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.commandBuffer.AddComponent(this.entity, components);
        }

        [BurstDiscard]
        public void AddComponentObject<T>(in T component)
            where T : class
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            throw new NotImplementedException("Can't AddComponentObject from a command buffer");
#endif
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.commandBuffer.SetComponent(this.entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            return this.commandBuffer.AddBuffer<T>(this.entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            return this.commandBuffer.SetBuffer<T>(this.entity);
        }

        public void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData
        {
            this.commandBuffer.AppendToBuffer(this.entity, element);
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.commandBuffer.SetComponentEnabled<T>(this.entity, enabled);
        }
    }
}
