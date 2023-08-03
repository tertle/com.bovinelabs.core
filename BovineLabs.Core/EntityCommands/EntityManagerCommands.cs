// <copyright file="EntityManagerCommands.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.EntityCommands
{
    using Unity.Entities;

    public struct EntityManagerCommands : IEntityCommands
    {
        private Entity entity;
        private EntityManager entityManager;
        private BlobAssetStore blobAssetStore;

        public EntityManagerCommands(EntityManager entityManager, Entity entity, BlobAssetStore blobAssetStore = default)
        {
            this.entityManager = entityManager;
            this.entity = entity;
            this.blobAssetStore = blobAssetStore;
        }

        public EntityManagerCommands(EntityManager entityManager, BlobAssetStore blobAssetStore = default)
        {
            this.entityManager = entityManager;
            this.blobAssetStore = blobAssetStore;

            this.entity = entityManager.CreateEntity();
        }

        public Entity Create()
        {
            this.entity = this.entityManager.CreateEntity();
            return this.entity;
        }

        public Entity Instantiate(Entity prefab)
        {
            this.entity = this.entityManager.Instantiate(prefab);
            return prefab;
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
            this.entityManager.AddComponent<T>(this.entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.entityManager.AddComponentData(this.entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            this.entityManager.AddComponent(this.entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.entityManager.SetComponentData(this.entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.entityManager.AddBuffer<T>(this.entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            var buffer = this.entityManager.GetBuffer<T>(this.entity);
            buffer.Clear();
            return buffer;
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.entityManager.SetComponentEnabled<T>(this.entity, enabled);
        }
    }
}
