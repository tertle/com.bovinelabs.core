// <copyright file="IEntityCommands.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.EntityCommands
{
    using Unity.Burst;
    using Unity.Entities;

    public interface IEntityCommands
    {
        Entity Entity { get; set; }

        /// <summary> Creates a new entity and replaces any internal stored one so other commands will now affect this. </summary>
        /// <returns> The new entity. </returns>
        Entity CreateEntity();

        Entity Instantiate(Entity prefab);

        void AddBlobAsset<T>(ref BlobAssetReference<T> blobAssetReference, out Hash128 objectHash)
            where T : unmanaged;

        void AddComponent<T>()
            where T : unmanaged, IComponentData;

        void AddComponent<T>(in T component)
            where T : unmanaged, IComponentData;

        void AddComponent(in ComponentTypeSet components);

        void AddComponentObject<T>(in T component)
            where T : class;

        void SetComponent<T>(in T component)
            where T : unmanaged, IComponentData;

        DynamicBuffer<T> AddBuffer<T>()
            where T : unmanaged, IBufferElementData;

        DynamicBuffer<T> SetBuffer<T>()
            where T : unmanaged, IBufferElementData;

        void AppendToBuffer<T>(in T element)
            where T : unmanaged, IBufferElementData;

        void SetComponentEnabled<T>(bool enabled)
            where T : unmanaged, IEnableableComponent;
    }
}
