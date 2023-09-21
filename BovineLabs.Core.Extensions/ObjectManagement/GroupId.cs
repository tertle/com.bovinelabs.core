// <copyright file="GroupId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using Unity.Entities;

    /// <summary> The ID of an object. </summary>
    [Serializable]
    public struct GroupId : IComponentData, IEquatable<GroupId>, IComparable<GroupId>
    {
        public short ID;

        public static implicit operator short(GroupId id)
        {
            return id.ID;
        }

        public static implicit operator GroupId(short id)
        {
            return new GroupId { ID = id };
        }

        /// <inheritdoc />
        public bool Equals(GroupId other)
        {
            return this.ID == other.ID;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.ID;
        }

        public int CompareTo(GroupId other)
        {
            return this.ID.CompareTo(other.ID);
        }
    }
}
#endif
