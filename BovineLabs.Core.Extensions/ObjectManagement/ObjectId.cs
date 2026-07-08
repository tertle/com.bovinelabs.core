// <copyright file="ObjectId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using BovineLabs.Core.Asset;
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
        public const int MaxModsIds = BLId.MaxModsIds;

        public static readonly ObjectId Null = default;

        [UnityEngine.SerializeField]
        private BLId value;

        public ObjectId(int id, ushort mod = 0)
        {
            this.value = new BLId(id, mod);
        }

        public ObjectId(BLId id)
        {
            this.value = id;
        }

        [CreateProperty]
        public int RawValue
        {
            readonly get => this.value.RawValue;
            set => this.value.RawValue = value;
        }

        [CreateProperty]
        public readonly ushort Mod => this.value.Mod;

        [CreateProperty]
        public readonly int ID => this.value.ID;

        public static implicit operator BLId(ObjectId id)
        {
            return id.value;
        }

        public static explicit operator ObjectId(BLId id)
        {
            return new ObjectId(id);
        }

        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return left.value == right.value;
        }

        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return left.value != right.value;
        }

        public readonly ObjectId WithMod(ushort mod)
        {
            return new ObjectId(this.value.WithMod(mod));
        }

        /// <inheritdoc/>
        public readonly int CompareTo(ObjectId other)
        {
            return this.value.CompareTo(other.value);
        }

        /// <inheritdoc/>
        public override readonly string ToString()
        {
            return $"ID:{this.ID}";
        }

        public readonly FixedString32Bytes ToFixedString()
        {
            return $"ID:{this.ID}";
        }

        /// <inheritdoc />
        public override readonly bool Equals(object obj)
        {
            return obj is ObjectId other && this.Equals(other);
        }

        /// <inheritdoc />
        public readonly bool Equals(ObjectId other)
        {
            return this.value.Equals(other.value);
        }

        /// <inheritdoc />
        public override readonly int GetHashCode()
        {
            return this.value.GetHashCode();
        }
    }
}
#endif
