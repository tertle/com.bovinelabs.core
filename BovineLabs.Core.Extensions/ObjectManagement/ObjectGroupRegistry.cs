// <copyright file="ObjectGroupRegistry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using BovineLabs.Core.Iterators;
    using JetBrains.Annotations;
    using Unity.Entities;

    [InternalBufferCapacity(0)]
    public struct ObjectGroupRegistry : IDynamicMultiHashMap<GroupId, ObjectId>
    {
        /// <inheritdoc />
        [UsedImplicitly]
        byte IDynamicMultiHashMap<GroupId, ObjectId>.Value { get; }
    }
}
#endif
