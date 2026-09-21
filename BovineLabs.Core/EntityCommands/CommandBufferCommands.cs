namespace BovineLabs.Core.EntityCommands
{
    using Unity.Collections;
    using Unity.Entities;

    public struct CommandBufferCommands : IEntityCommands
    {
        private EntityCommandBuffer _commandBuffer;
        private BlobAssetStore _blobAssetStore;

        public CommandBufferCommands(EntityCommandBuffer commandBuffer, Entity localEntity = default, BlobAssetStore blobAssetStore = default)
        {
            _commandBuffer = commandBuffer;
            Entity = localEntity;
            _blobAssetStore = blobAssetStore;
        }

        public Entity Entity { get; set; }

        public Entity CreateEntity()
        {
            Entity = _commandBuffer.CreateEntity();
            return Entity;
        }

        public Entity Instantiate(Entity prefab)
        {
            Entity = _commandBuffer.Instantiate(prefab);
            return Entity;
        }

        public void SetName(FixedString64Bytes name)
        {
            _commandBuffer.SetName(Entity, name);
        }

        public void SetName(Entity entity, FixedString64Bytes name)
        {
            _commandBuffer.SetName(entity, name);
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
            _commandBuffer.AddComponent<T>(entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            AddComponent(Entity, component);
        }

        public void AddComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            _commandBuffer.AddComponent(entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            AddComponent(Entity, components);
        }

        public void AddComponent(Entity entity, in ComponentTypeSet components)
        {
            _commandBuffer.AddComponent(entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            SetComponent(Entity, component);
        }

        public void SetComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            _commandBuffer.SetComponent(entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return AddBuffer<T>(Entity);
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return _commandBuffer.AddBuffer<T>(entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return SetBuffer<T>(Entity);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return _commandBuffer.SetBuffer<T>(entity);
        }

        public void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData
        {
            AppendToBuffer(Entity, element);
        }

        public void AppendToBuffer<T>(Entity entity, in T element)
            where T : unmanaged, IBufferElementData
        {
            _commandBuffer.AppendToBuffer(entity, element);
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            SetComponentEnabled<T>(Entity, enabled);
        }

        public void SetComponentEnabled<T>(Entity entity, bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            _commandBuffer.SetComponentEnabled<T>(entity, enabled);
        }

        public void AddSharedComponent<T>(Entity entity, in T component)
            where T : unmanaged, ISharedComponentData
        {
            _commandBuffer.AddSharedComponent(entity, component);
        }

        public void SetSharedComponent<T>(Entity entity, in T component)
            where T : unmanaged, ISharedComponentData
        {
            _commandBuffer.SetSharedComponent(entity, component);
        }
    }
}
