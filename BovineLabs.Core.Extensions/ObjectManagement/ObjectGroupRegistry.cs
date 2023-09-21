// <copyright file="ObjectGroupRegistry.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
{
    using BovineLabs.Core.Iterators;
    using Unity.Entities;

    public struct ObjectGroupRegistry : IDynamicMultiHashMap<GroupId, ObjectId>
    {
        byte IDynamicMultiHashMap<GroupId, ObjectId>.Value { get; }
    }

    public static class ObjectGroupRegistryExtensions
    {
        public static DynamicMultiHashMap<GroupId, ObjectId> AsMap(this DynamicBuffer<ObjectGroupRegistry> buffer)
        {
            return buffer.AsMultiHashMap<ObjectGroupRegistry, GroupId, ObjectId>();
        }

        internal static DynamicBuffer<ObjectGroupRegistry> Initialize(this DynamicBuffer<ObjectGroupRegistry> buffer)
        {
            return buffer.InitializeMultiHashMap<ObjectGroupRegistry, GroupId, ObjectId>();
        }
    }
}
