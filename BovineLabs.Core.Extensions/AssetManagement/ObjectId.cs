// <copyright file="ObjectId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.AssetManagement
{
    using System;
    using Unity.Entities;

    /// <summary> The ID of an object. </summary>
    [Serializable]
    public struct ObjectId : IComponentData, IEquatable<ObjectId>, IComparable<ObjectId>
    {
        public int ID;

        public static implicit operator int(ObjectId id)
        {
            return id.ID;
        }

        /// <inheritdoc />
        public bool Equals(ObjectId other)
        {
            return this.ID == other.ID;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.ID;
        }

        public int CompareTo(ObjectId other)
        {
            return this.ID.CompareTo(other.ID);
        }
    }
}
