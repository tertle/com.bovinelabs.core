// <copyright file="BLId.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ObjectManagement
{
    using System;
    using Unity.Properties;

    /// <summary> Packed mod/local id payload shared by type-safe id wrappers. </summary>
    [Serializable]
    public struct BLId : IEquatable<BLId>, IComparable<BLId>
    {
        private const int ModBits = 10;
        private const int ModShift = 32 - ModBits;

        public const int MaxModsIds = 1 << ModBits;
        public const int MaxLocalIds = 1 << ModShift;
        public const int MaxLocalId = MaxLocalIds - 1;

        private const int IDMask = MaxLocalId;

        public static readonly BLId Null = default;

        public int RawValue;

        public BLId(int id, ushort mod = 0)
        {
#if UNITY_EDITOR
            if (id < 0 || id > IDMask)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Id out of range");
            }

            if (mod >= MaxModsIds)
            {
                throw new ArgumentOutOfRangeException(nameof(mod), "Mod id too large");
            }
#endif

            this.RawValue = id == 0 ? 0 : mod << ModShift | id;
        }

        [CreateProperty]
        public readonly ushort Mod => (ushort)((uint)this.RawValue >> ModShift);

        [CreateProperty]
        public readonly int ID => this.RawValue & IDMask;

        [CreateProperty]
        public readonly bool IsNull => this.ID == 0;

        public static bool operator ==(BLId left, BLId right)
        {
            return left.RawValue == right.RawValue;
        }

        public static bool operator !=(BLId left, BLId right)
        {
            return left.RawValue != right.RawValue;
        }

        public readonly BLId WithMod(ushort mod)
        {
            return new BLId(this.ID, mod);
        }

        /// <inheritdoc/>
        public readonly int CompareTo(BLId other)
        {
            var modCompare = this.Mod.CompareTo(other.Mod);
            return modCompare != 0 ? modCompare : this.ID.CompareTo(other.ID);
        }

        /// <inheritdoc/>
        public override readonly bool Equals(object obj)
        {
            return obj is BLId other && this.Equals(other);
        }

        /// <inheritdoc/>
        public readonly bool Equals(BLId other)
        {
            return this.RawValue == other.RawValue;
        }

        /// <inheritdoc/>
        public override readonly int GetHashCode()
        {
            return this.RawValue;
        }

        /// <inheritdoc/>
        public override readonly string ToString()
        {
            return this.Mod == 0 ? $"ID:{this.ID}" : $"Mod:{this.Mod} ID:{this.ID}";
        }
    }
}
