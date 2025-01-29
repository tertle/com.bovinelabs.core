// <copyright file="ObjectDefinitionRegistry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using Unity;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Properties;

    /// <summary> A buffer of all objects in the project where <see cref="ObjectDefinition.ID" /> maps to the index. </summary>
    [InternalBufferCapacity(0)]
    public readonly struct ObjectDefinitionRegistry : IComponentData
    {
        [ReadOnly]
        private readonly NativeHashMap<int, int> objectDefinitionsOffsets;

        [ReadOnly]
        private readonly NativeList<Entity> objectDefinitions;

        internal ObjectDefinitionRegistry(NativeList<Entity> objectDefinitions, NativeHashMap<int, int> objectDefinitionsOffsets)
        {
            this.objectDefinitions = objectDefinitions;
            this.objectDefinitionsOffsets = objectDefinitionsOffsets;
        }

        [CreateProperty]
        private Entity[] ObjectDefinitions => this.objectDefinitions.AsArray().ToArray();

        public Entity this[ObjectId id]
        {
            get
            {
                if (!this.objectDefinitionsOffsets.TryGetValue(id.Mod, out var offset))
                {
                    Debug.LogWarning($"Loading object from mod {id.Mod} that wasn't loaded");
                    return Entity.Null;
                }

                var index = offset + id.ID;

                if (index < 0 || index >= this.objectDefinitions.Length)
                {
                    Debug.LogWarning($"Index for ObjectId {id.Mod} {id.ID} was out of range");
                    return Entity.Null;
                }

                return this.objectDefinitions[offset + id.ID];
            }
        }
    }
}
#endif
