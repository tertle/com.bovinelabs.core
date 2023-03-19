// <copyright file="EntityManagerConverter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Convert
{
    using Unity.Entities;

    public struct EntityManagerConverter : IConvert
    {
        private readonly Entity entity;
        private EntityManager entityManager;
        private BlobAssetStore blobAssetStore;

        public EntityManagerConverter(EntityManager entityManager, Entity entity, BlobAssetStore blobAssetStore = default)
        {
            this.entityManager = entityManager;
            this.entity = entity;
            this.blobAssetStore = blobAssetStore;
        }

        public EntityManagerConverter(EntityManager entityManager, BlobAssetStore blobAssetStore = default)
        {
            this.entityManager = entityManager;
            this.blobAssetStore = blobAssetStore;

            this.entity = entityManager.CreateEntity();
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
    }
}
