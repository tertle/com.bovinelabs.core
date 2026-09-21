namespace BovineLabs.Core.EntityCommands
{
    using Unity.Collections;
    using Unity.Entities;

    public struct CommandBufferParallelCommands : IEntityCommands
    {
        private readonly int _sortKey;
        private EntityCommandBuffer.ParallelWriter _commandBuffer;
        private BlobAssetStore _blobAssetStore;

        public CommandBufferParallelCommands(
            EntityCommandBuffer.ParallelWriter commandBuffer, int sortKey, Entity localEntity = default, BlobAssetStore blobAssetStore = default)
        {
            _commandBuffer = commandBuffer;
            _sortKey = sortKey;
            Entity = localEntity;
            _blobAssetStore = blobAssetStore;
        }

        public Entity Entity { get; set; }

        public Entity CreateEntity()
        {
            Entity = _commandBuffer.CreateEntity(_sortKey);
            return Entity;
        }

        public Entity Instantiate(Entity prefab)
        {
            Entity = _commandBuffer.Instantiate(_sortKey, prefab);
            return Entity;
        }

        public void SetName(FixedString64Bytes name)
        {
            _commandBuffer.SetName(_sortKey, Entity, name);
        }

        public void SetName(Entity entity, FixedString64Bytes name)
        {
            _commandBuffer.SetName(_sortKey, entity, name);
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
            _commandBuffer.AddComponent<T>(_sortKey, entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            AddComponent(Entity, component);
        }

        public void AddComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            _commandBuffer.AddComponent(_sortKey, entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            AddComponent(Entity, components);
        }

        public void AddComponent(Entity entity, in ComponentTypeSet components)
        {
            _commandBuffer.AddComponent(_sortKey, entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            SetComponent(Entity, component);
        }

        public void SetComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            _commandBuffer.SetComponent(_sortKey, entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return AddBuffer<T>(Entity);
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return _commandBuffer.AddBuffer<T>(_sortKey, entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return SetBuffer<T>(Entity);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return _commandBuffer.SetBuffer<T>(_sortKey, entity);
        }

        public void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData
        {
            AppendToBuffer(Entity, element);
        }

        public void AppendToBuffer<T>(Entity entity, in T element)
            where T : unmanaged, IBufferElementData
        {
            _commandBuffer.AppendToBuffer(_sortKey, entity, element);
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            SetComponentEnabled<T>(Entity, enabled);
        }

        public void SetComponentEnabled<T>(Entity entity, bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            _commandBuffer.SetComponentEnabled<T>(_sortKey, entity, enabled);
        }

        public void AddSharedComponent<T>(Entity entity, in T component)
            where T : unmanaged, ISharedComponentData
        {
            _commandBuffer.AddSharedComponent(_sortKey, entity, component);
        }

        public void SetSharedComponent<T>(Entity entity, in T component)
            where T : unmanaged, ISharedComponentData
        {
            _commandBuffer.SetSharedComponent(_sortKey, entity, component);
        }
    }
}
