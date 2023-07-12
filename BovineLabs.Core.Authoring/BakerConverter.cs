// <copyright file="BakerConverter.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using BovineLabs.Core.Convert;
    using Unity.Entities;

    public readonly struct BakerConverter : IConvert
    {
        private readonly IBaker baker;
        private readonly Entity entity;

        public BakerConverter(IBaker baker, Entity entity)
        {
            this.baker = baker;
            this.entity = entity;
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

        public void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent
        {
            this.baker.SetComponentEnabled<T>(this.entity, enabled);
        }
    }
}
