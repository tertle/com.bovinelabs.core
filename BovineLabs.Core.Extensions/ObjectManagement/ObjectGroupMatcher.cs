// <copyright file="ObjectGroupMatcher.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using BovineLabs.Core.Iterators;
    using JetBrains.Annotations;
    using Unity.Entities;
    using Unity.Properties;

    /// <summary>
    /// A <see cref="DynamicHashSet{TKey}" /> of (<see cref="GroupId" />, <see cref="ObjectId" />) pairs that can be used to check if an object belongs to a group.
    /// </summary>
    [InternalBufferCapacity(32)]
    public struct ObjectGroupMatcher : IDynamicHashSet<ObjectGroupKey>
    {
        /// <inheritdoc />
        [UsedImplicitly]
        [CreateProperty]
        byte IDynamicHashSet<ObjectGroupKey>.Value { get; }
    }

    /// <summary> Key value pair of <see cref="GroupId" /> and <see cref="ObjectId" /> used as in <see cref="ObjectGroupMatcher" />. </summary>
    public struct ObjectGroupKey : IEquatable<ObjectGroupKey>, IEquatable<(GroupId GroupId, ObjectId ObjectId)>
    {
        public GroupId GroupId;
        public ObjectId ObjectId;

        public static implicit operator ObjectGroupKey((GroupId GroupId, ObjectId ObjectId) tuple)
        {
            return new ObjectGroupKey
            {
                GroupId = tuple.GroupId,
                ObjectId = tuple.ObjectId,
            };
        }

        public bool Equals(ObjectGroupKey other)
        {
            return this.GroupId.Equals(other.GroupId) && this.ObjectId.Equals(other.ObjectId);
        }

        public bool Equals((GroupId GroupId, ObjectId ObjectId) other)
        {
            return this.GroupId.Equals(other.GroupId) && this.ObjectId.Equals(other.ObjectId);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (this.GroupId.GetHashCode() * 397) ^ this.ObjectId.GetHashCode();
            }
        }
    }

    /// <summary> Convenient extensions for <see cref="ObjectGroupMatcher" />. </summary>
    public static partial class ObjectGroupMatcherExtensions
    {
        /// <summary> Checks if <see cref="key" /> <see cref="ObjectId" /> is a match inside the group with <see cref="key" /> <see cref="GroupId" />. </summary>
        /// <param name="buffer"> The ObjectGroupMatch singleton buffer. </param>
        /// <param name="key"> The <see cref="ObjectId" /> to match to the <see cref="GroupId" />. </param>
        /// <returns> True if it's part of the group, otherwise false. </returns>
        public static bool Matches(this DynamicBuffer<ObjectGroupMatcher> buffer, ObjectGroupKey key)
        {
            return buffer.AsMap().Contains(key);
        }

        /// <summary> Checks if <see cref="key" /> ObjectID is a match inside the group with <see cref="key" /> <see cref="GroupId" />. </summary>
        /// <param name="buffer"> The ObjectGroupMatch singleton buffer. </param>
        /// <param name="key"> The <see cref="ObjectId" /> to match to the <see cref="GroupId" />. </param>
        /// <returns> True if it's part of the group, otherwise false. </returns>
        public static bool Matches(this DynamicBuffer<ObjectGroupMatcher> buffer, (GroupId GroupId, ObjectId ObjectId) key)
        {
            return buffer.AsMap().Contains(key);
        }

        /// <summary> Checks if <see cref="objectId" /> is a match inside the group with <see cref="GroupId" />. </summary>
        /// <param name="buffer"> The ObjectGroupMatch singleton buffer. </param>
        /// <param name="groupId"> The group to check. </param>
        /// <param name="objectId"> The object id to check. </param>
        /// <returns> True if it's part of the group, otherwise false. </returns>
        public static bool Matches(this DynamicBuffer<ObjectGroupMatcher> buffer, GroupId groupId, ObjectId objectId)
        {
            return buffer
            .AsMap()
            .Contains(new ObjectGroupKey
            {
                GroupId = groupId,
                ObjectId = objectId,
            });
        }
    }
}
#endif
