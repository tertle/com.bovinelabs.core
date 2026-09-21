namespace BovineLabs.Core.Authoring.EntityCommands
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using Unity.Collections;
    using Unity.Entities;

    public struct BakerCommands : IEntityCommands
    {
        private readonly IBaker _baker;

        public BakerCommands(IBaker baker, Entity localEntity)
        {
            _baker = baker;
            Entity = localEntity;
        }

        public Entity Entity { get; set; }

        public Entity CreateEntity()
        {
            Entity = _baker.CreateAdditionalEntity(TransformUsageFlags.None);
            return Entity;
        }

        public Entity Instantiate(Entity prefab)
        {
            throw new NotImplementedException("Can't instantiate from a baker");
        }

        public void SetName(FixedString64Bytes name)
        {
            throw new NotImplementedException("Can't SetName from a baker");
        }

        public void SetName(Entity entity, FixedString64Bytes name)
        {
            throw new NotImplementedException("Can't SetName from a baker");
        }

        public void AddBlobAsset<T>(ref BlobAssetReference<T> blobAssetReference, out Hash128 objectHash)
            where T : unmanaged
        {
            _baker.AddBlobAsset(ref blobAssetReference, out objectHash);
        }

        public void AddComponent<T>()
            where T : unmanaged, IComponentData
        {
            AddComponent<T>(Entity);
        }

        public void AddComponent<T>(Entity entity)
            where T : unmanaged, IComponentData
        {
            _baker.AddComponent<T>(entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            AddComponent(Entity, component);
        }

        public void AddComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            _baker.AddComponent(entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            AddComponent(Entity, components);
        }

        public void AddComponent(Entity entity, in ComponentTypeSet components)
        {
            _baker.AddComponent(entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            SetComponent(Entity, component);
        }

        public void SetComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            _baker.SetComponent(entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return AddBuffer<T>(Entity);
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return _baker.AddBuffer<T>(entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return SetBuffer<T>(Entity);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return _baker.SetBuffer<T>(entity);
        }

        public void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData
        {
            AppendToBuffer(Entity, element);
        }

        public void AppendToBuffer<T>(Entity entity, in T element)
            where T : unmanaged, IBufferElementData
        {
            throw new NotImplementedException("Can't append to buffer in a baker, use Add/Set");
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            SetComponentEnabled<T>(Entity, enabled);
        }

        public void SetComponentEnabled<T>(Entity entity, bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            _baker.SetComponentEnabled<T>(entity, enabled);
        }

        public void AddSharedComponent<T>(Entity entity, in T component)
            where T : unmanaged, ISharedComponentData
        {
            _baker.AddSharedComponent(entity, component);
        }

        public void SetSharedComponent<T>(Entity entity, in T component)
            where T : unmanaged, ISharedComponentData
        {
            _baker.SetSharedComponent(entity, component);
        }
    }
}
