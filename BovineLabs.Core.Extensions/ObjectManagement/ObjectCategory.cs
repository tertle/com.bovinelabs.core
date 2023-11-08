// <copyright file="ObjectCategory.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using Unity.Entities;

    // TODO is this needed?
    [Serializable]
    public struct ObjectCategory : IComponentData, IEquatable<ObjectCategory>, IComparable<ObjectCategory>
    {
        public const int MaxBits = 32;

        // Flags
        public uint Value;

        public static implicit operator uint(ObjectCategory category)
        {
            return category.Value;
        }

        public static implicit operator ObjectCategory(uint category)
        {
            return new ObjectCategory { Value = category };
        }

        /// <inheritdoc />
        public bool Equals(ObjectCategory other)
        {
            return this.Value == other.Value;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.Value.GetHashCode();
        }

        public int CompareTo(ObjectCategory other)
        {
            return this.Value.CompareTo(other.Value);
        }
    }
}
#endif
