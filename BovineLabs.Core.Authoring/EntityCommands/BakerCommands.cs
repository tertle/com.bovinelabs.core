// <copyright file="BakerCommands.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.EntityCommands
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using Unity.Collections;
    using Unity.Entities;

    public struct BakerCommands : IEntityCommands
    {
        private readonly IBaker baker;

        public BakerCommands(IBaker baker, Entity localEntity)
        {
            this.baker = baker;
            this.Entity = localEntity;
        }

        public Entity Entity { get; set; }

        public Entity CreateEntity()
        {
            this.Entity = this.baker.CreateAdditionalEntity(TransformUsageFlags.None);
            return this.Entity;
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
            this.baker.AddBlobAsset(ref blobAssetReference, out objectHash);
        }

        public void AddComponent<T>()
            where T : unmanaged, IComponentData
        {
            this.AddComponent<T>(this.Entity);
        }

        public void AddComponent<T>(Entity entity)
            where T : unmanaged, IComponentData
        {
            this.baker.AddComponent<T>(entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.AddComponent(this.Entity, component);
        }

        public void AddComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            this.baker.AddComponent(entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            this.AddComponent(this.Entity, components);
        }

        public void AddComponent(Entity entity, in ComponentTypeSet components)
        {
            this.baker.AddComponent(entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.SetComponent(this.Entity, component);
        }

        public void SetComponent<T>(Entity entity, in T component)
            where T : unmanaged, IComponentData
        {
            this.baker.SetComponent(entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.AddBuffer<T>(this.Entity);
        }

        public DynamicBuffer<T> AddBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return this.baker.AddBuffer<T>(entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.SetBuffer<T>(this.Entity);
        }

        public DynamicBuffer<T> SetBuffer<T>(Entity entity)
            where T : unmanaged, IBufferElementData
        {
            return this.baker.SetBuffer<T>(entity);
        }

        public void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData
        {
            this.AppendToBuffer(this.Entity, element);
        }

        public void AppendToBuffer<T>(Entity entity, in T element)
            where T : unmanaged, IBufferElementData
        {
            throw new NotImplementedException("Can't append to buffer in a baker, use Add/Set");
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.SetComponentEnabled<T>(this.Entity, enabled);
        }

        public void SetComponentEnabled<T>(Entity entity, bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.baker.SetComponentEnabled<T>(entity, enabled);
        }
    }
}
