namespace BovineLabs.Core.EntityCommands
{
    using Unity.Collections;
    using Unity.Entities;

    public struct EntityManagerCommands : IEntityCommands
    {
        private EntityManager _entityManager;
        private BlobAssetStore _blobAssetStore;

        public EntityManagerCommands(EntityManager entityManager, Entity localEntity = default, BlobAssetStore blobAssetStore = default)
        {
            _entityManager = entityManager;
            Entity = localEntity;
            _blobAssetStore = blobAssetStore;
        }

        public Entity Entity { get; set; }

        public Entity CreateEntity()
        {
            Entity = _entityManager.CreateEntity();
            return Entity;
        }

        public Entity Instantiate(Entity prefab)
        {
            Entity = _entityManager.Instantiate(prefab);
            return Entity;
        }

        public void SetName(FixedString64Bytes name)
        {
            _entityManager.SetName(Entity, name);
        }

        public void SetName(Entity entity, FixedString64Bytes name)
        {
            _entityManager.SetName(entity, name);
        }

        public void AddBlobAsset<T>(ref BlobAssetReference<T> blobAssetReference, out Hash128 objectHash)
            where T : unmanaged
        {
            if (_blobAssetStore.IsCreated)
            {
                _blobAssetStore.TryAdd(ref blobAssetReference, out objectHash);
            }
            else
            {
                objectHash = default;
            }
        }

        public void AddComponent<T>()
            where T : unmanaged, IComponentData
        {
            AddComponent<T>(Entity);
        }

        public void AddComponent<T>(Entity entity)
            where T : unmanaged, IComponentData
        {
            _entityManager.AddComponent<T>(entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            AddComponent(Entity, component);
        }

        public void AddComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            _entityManager.AddComponentData(entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            AddComponent(Entity, components);
        }

        public void AddComponent(Entity entity, in ComponentTypeSet components)
        {
            _entityManager.AddComponent(entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            SetComponent(Entity, component);
        }

        public void SetComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            _entityManager.SetComponentData(entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return AddBuffer<T>(Entity);
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return _entityManager.AddBuffer<T>(entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return SetBuffer<T>(Entity);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            var buffer = _entityManager.GetBuffer<T>(entity);
            buffer.Clear();
            return buffer;
        }

        public void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData
        {
            AppendToBuffer(Entity, element);
        }

        public void AppendToBuffer<T>(Entity entity, in T element)
            where T : unmanaged, IBufferElementData
        {
            _entityManager.GetBuffer<T>(entity).Add(element);
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            SetComponentEnabled<T>(Entity, enabled);
        }

        public void SetComponentEnabled<T>(Entity entity, bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            _entityManager.SetComponentEnabled<T>(entity, enabled);
        }

        public void AddSharedComponent<T>(Entity entity, in T component)
            where T : unmanaged, ISharedComponentData
        {
            _entityManager.AddSharedComponent(entity, component);
        }

        public void SetSharedComponent<T>(Entity entity, in T component)
            where T : unmanaged, ISharedComponentData
        {
            _entityManager.SetSharedComponent(entity, component);
        }
    }
}
