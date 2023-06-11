// <copyright file="ObjectId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using Unity.Entities;

    /// <summary>
    /// Wrapper for the ID of an object. This can be used to store weak references to entities
    /// that can be instantiated at runtime via <see cref="ObjectDefinitionRegistry"/>. </summary>
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
#endif
