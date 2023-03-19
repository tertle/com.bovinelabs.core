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

        public BakerConverter(IBaker baker)
        {
            this.baker = baker;
        }

        public void AddBlobAsset<T>(ref BlobAssetReference<T> blobAssetReference, out Hash128 objectHash)
            where T : unmanaged
        {
            this.baker.AddBlobAsset(ref blobAssetReference, out objectHash);
        }

        public void AddComponent<T>()
            where T : unmanaged, IComponentData
        {
            this.baker.AddComponent<T>();
        }

        public void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.baker.AddComponent(component);
        }

        public void AddComponent(in ComponentTypeSet components)
        {
            this.baker.AddComponent(components);
        }

        public void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData
        {
            this.baker.SetComponent(component);
        }

        public DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData
        {
            return this.baker.AddBuffer<T>();
        }
    }
}
