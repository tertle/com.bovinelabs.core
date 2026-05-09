// <copyright file="ObjectId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Properties;

    /// <summary>
    /// Wrapper for the ID of an object. This can be used to store weak references to entities
    /// that can be instantiated at runtime via <see cref="ObjectDefinitionRegistry" />.
    /// </summary>
    [Serializable]
    public struct ObjectId : IComponentData, IEquatable<ObjectId>, IComparable<ObjectId>
    {
        public const int MaxModsIds = 1 << ModBytes;

        private const int ModBytes = 10;
        private const int ModShift = 32 - ModBytes;
        private const int IDMask = (1 << ModShift) - 1;

        public static readonly ObjectId Null = default;

        public int RawValue;

        public ObjectId(int id, ushort mod = 0)
        {
#if UNITY_EDITOR
            if (id > IDMask)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Id too large");
            }

            if (mod >= MaxModsIds)
            {
                throw new ArgumentOutOfRangeException(nameof(mod), "Mod id too large");
            }
#endif

            this.RawValue = mod << ModShift | id;
        }

        [CreateProperty]
        public ushort Mod => (ushort)(this.RawValue >> ModShift);

        [CreateProperty]
        public int ID => this.RawValue & IDMask;

        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return left.ID == right.ID;
        }

        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return left.ID != right.ID;
        }

        /// <inheritdoc/>
        public int CompareTo(ObjectId other)
        {
            return this.RawValue.CompareTo(other.RawValue);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"ID:{this.ID}";
        }

        public FixedString32Bytes ToFixedString()
        {
            return $"ID:{this.ID}";
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ObjectId other && this.Equals(other);
        }

        /// <inheritdoc />
        public bool Equals(ObjectId other)
        {
            return this.RawValue == other.RawValue;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.RawValue;
        }
    }
}
#endif
