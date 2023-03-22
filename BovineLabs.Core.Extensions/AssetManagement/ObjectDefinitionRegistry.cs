// <copyright file="ObjectDefinitionRegistry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.AssetManagement
{
    using Unity.Entities;

    /// <summary> A buffer of all objects in the project where <see cref="ObjectDefinition.ID" /> maps to the index. </summary>
    [InternalBufferCapacity(0)]
    public struct ObjectDefinitionRegistry : IBufferElementData
    {
        public Entity Prefab;
    }
}
#endif
