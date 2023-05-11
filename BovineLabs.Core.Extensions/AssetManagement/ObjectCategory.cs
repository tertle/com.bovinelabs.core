// <copyright file="ObjectCategory.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.AssetManagement
{
    using System;
    using Unity.Collections;
    using Unity.Entities;

    public struct ObjectCategory : IComponentData, IEquatable<ObjectCategory>, IComparable<ObjectCategory>
    {
        public const int MaxBits = 64;

        // Flags
        public ulong Value;

        public static implicit operator ulong(ObjectCategory category)
        {
            return category.Value;
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
