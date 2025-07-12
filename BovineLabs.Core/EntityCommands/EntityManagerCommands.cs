// <copyright file="EntityManagerCommands.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.EntityCommands
{
    using Unity.Collections;
    using Unity.Entities;

    public struct EntityManagerCommands : IEntityCommands
    {
        private EntityManager entityManager;
        private BlobAssetStore blobAssetStore;

        public EntityManagerCommands(EntityManager entityManager, Entity localEntity = default, BlobAssetStore blobAssetStore = default)
        {
            this.entityManager = entityManager;
            this.Entity = localEntity;
            this.blobAssetStore = blobAssetStore;
        }

        public Entity Entity { get; set; }

        public Entity CreateEntity()
        {
            this.Entity = this.entityManager.CreateEntity();
            return this.Entity;
        }

        public Entity Instantiate(Entity prefab)
        {
            this.Entity = this.entityManager.Instantiate(prefab);
            return this.Entity;
        }

        public void SetName(FixedString64Bytes name)
        {
            this.entityManager.SetName(this.Entity, name);
        }

        public void SetName(Entity entity, FixedString64Bytes name)
        {
            this.entityManager.SetName(entity, name);
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
            this.AddComponent<T>(this.Entity);
        }

        public void AddComponent<T>(Entity entity)
            where T : unmanaged, IComponentData
        {
            this.entityManager.AddComponent<T>(entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.AddComponent(this.Entity, component);
        }

        public void AddComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            this.entityManager.AddComponentData(entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            this.AddComponent(this.Entity, components);
        }

        public void AddComponent(Entity entity, in ComponentTypeSet components)
        {
            this.entityManager.AddComponent(entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.SetComponent(this.Entity, component);
        }

        public void SetComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            this.entityManager.SetComponentData(entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.AddBuffer<T>(this.Entity);
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return this.entityManager.AddBuffer<T>(entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.SetBuffer<T>(this.Entity);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            var buffer = this.entityManager.GetBuffer<T>(entity);
            buffer.Clear();
            return buffer;
        }

        public void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData
        {
            this.AppendToBuffer(this.Entity, element);
        }

        public void AppendToBuffer<T>(Entity entity, in T element)
            where T : unmanaged, IBufferElementData
        {
            this.entityManager.GetBuffer<T>(entity).Add(element);
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.SetComponentEnabled<T>(this.Entity, enabled);
        }

        public void SetComponentEnabled<T>(Entity entity, bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.entityManager.SetComponentEnabled<T>(entity, enabled);
        }
    }
}
