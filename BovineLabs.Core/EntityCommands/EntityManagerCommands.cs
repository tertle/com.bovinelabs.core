// <copyright file="EntityManagerCommands.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.EntityCommands
{
    using BovineLabs.Core.Assertions;
    using Unity.Entities;

    public struct EntityManagerCommands : IEntityCommands
    {
        private Entity entity;
        private EntityManager entityManager;
        private BlobAssetStore blobAssetStore;

        public EntityManagerCommands(EntityManager entityManager, Entity entity = default, BlobAssetStore blobAssetStore = default)
        {
            this.entityManager = entityManager;
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
            this.entity = this.entityManager.CreateEntity();
            return this.entity;
        }

        public Entity Instantiate(Entity prefab)
        {
            this.entity = this.entityManager.Instantiate(prefab);
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
            this.entityManager.AddComponent<T>(this.entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.entityManager.AddComponentData(this.entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.entityManager.AddComponent(this.entity, components);
        }

        public void AddComponentObject<T>(in T component)
            where T : class
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.entityManager.AddComponentObject(this.entity, component);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.entityManager.SetComponentData(this.entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            return this.entityManager.AddBuffer<T>(this.entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            var buffer = this.entityManager.GetBuffer<T>(this.entity);
            buffer.Clear();
            return buffer;
        }

        public void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData
        {
            this.entityManager.GetBuffer<T>(this.entity).Add(element);
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            Check.Assume(!this.entity.Equals(Entity.Null));
            this.entityManager.SetComponentEnabled<T>(this.entity, enabled);
        }
    }
}
