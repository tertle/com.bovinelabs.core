// <copyright file="BakerCommands.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring.EntityCommands
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using Unity.Entities;

    public struct BakerCommands : IEntityCommands
    {
        private readonly IBaker baker;
        private Entity entity;

        public BakerCommands(IBaker baker, Entity entity)
        {
            this.baker = baker;
            this.entity = entity;
        }

        public Entity Create()
        {
            this.entity = this.baker.CreateAdditionalEntity(TransformUsageFlags.None);
            return this.entity;
        }

        public Entity Instantiate(Entity prefab)
        {
            throw new NotImplementedException("Can't instantiate from a baker");
        }

        public void AddBlobAsset<T>(ref BlobAssetReference<T> blobAssetReference, out Hash128 objectHash)
            where T : unmanaged
        {
            this.baker.AddBlobAsset(ref blobAssetReference, out objectHash);
        }

        public void AddComponent<T>()
            where T : unmanaged, IComponentData
        {
            this.baker.AddComponent<T>(this.entity);
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.baker.AddComponent(this.entity, component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            this.baker.AddComponent(this.entity, components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.baker.SetComponent(this.entity, component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.baker.AddBuffer<T>(this.entity);
        }

        public DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.baker.SetBuffer<T>(this.entity);
        }

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.baker.SetComponentEnabled<T>(this.entity, enabled);
        }
    }
}
