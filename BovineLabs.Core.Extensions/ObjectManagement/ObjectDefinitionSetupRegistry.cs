// <copyright file="ObjectDefinitionSetupRegistry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using JetBrains.Annotations;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    internal struct ObjectDefinitionSetupRegistry : IBufferElementData
    {
        [UsedImplicitly] // By ObjectDefinitionSystem
        public Entity Prefab;
    }
}
#endif
