// <copyright file="ObjectDefinitionRegistry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using Unity.Collections;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct ObjectDefinitionRegistry : IComponentData
    {
        [ReadOnly]
        private NativeHashMap<ObjectId, Entity> objectDefinitions;

        public ObjectDefinitionRegistry(NativeHashMap<ObjectId, Entity> objectDefinitions)
        {
            this.objectDefinitions = objectDefinitions;
        }

        public Entity this[ObjectId id] => this.objectDefinitions[id];

        public bool TryGetValue(ObjectId id, out Entity entity)
        {
            return this.objectDefinitions.TryGetValue(id, out entity);
        }
    }
}
#endif
