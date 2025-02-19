// <copyright file="ObjectInstantiate.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using Unity.Entities;

    public struct ObjectInstantiate : IComponentData
    {
        public Entity Prefab;
    }
}
#endif
