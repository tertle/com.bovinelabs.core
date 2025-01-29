// <copyright file="ObjectId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using Unity.Entities;
    using Unity.Properties;

    /// <summary>
    /// Wrapper for the ID of an object. This can be used to store weak references to entities
    /// that can be instantiated at runtime via <see cref="ObjectDefinitionRegistry" />.
    /// </summary>
    [Serializable]
    public readonly struct ObjectId : IComponentData, IEquatable<ObjectId>, IComparable<ObjectId>
    {
        public ObjectId(int mod, int id)
        {
            this.Mod = mod;
            this.ID = id;
        }

        [CreateProperty]
        public int Mod { get; }

        [CreateProperty]
        public int ID { get; }

        public static bool operator ==(ObjectId left, ObjectId right)
        {
            return left.CompareTo(right) == 0;
        }

        public static bool operator !=(ObjectId left, ObjectId right)
        {
            return left.CompareTo(right) != 0;
        }

        public int CompareTo(ObjectId other)
        {
            var idComparison = this.ID.CompareTo(other.ID);
            return idComparison != 0 ? idComparison : this.Mod.CompareTo(other.Mod);
        }

        public override string ToString()
        {
            return $"Mod:{this.Mod}, ID:{this.ID}";
        }

        /// <inheritdoc />
        public bool Equals(ObjectId other)
        {
            return this.ID == other.ID;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is ObjectId other && this.Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return this.ID;
        }
    }
}
#endif
