// <copyright file="WorldObjectRegistry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using BovineLabs.Core.Iterators;
    using JetBrains.Annotations;
    using Unity.Entities;

    /// <summary> Singleton dynamic multi-hash-map from object ids to currently live entities. </summary>
    [InternalBufferCapacity(0)]
    public struct WorldObjectRegistry : IDynamicMultiHashMap<ObjectId, Entity>
    {
        /// <inheritdoc />
        [UsedImplicitly]
        byte IDynamicMultiHashMap<ObjectId, Entity>.Value { get; }
    }

    public static partial class WorldObjectRegistryExtensions
    {
        public static bool TryGetFirstValue(this DynamicBuffer<WorldObjectRegistry> registry, ObjectId id, out Entity entity)
        {
            return registry.AsMultiHashMap<WorldObjectRegistry, ObjectId, Entity>().TryGetFirstValue(id, out entity, out _);
        }
    }
}
#endif
