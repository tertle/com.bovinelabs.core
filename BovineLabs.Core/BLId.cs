namespace BovineLabs.Core
{
    using System;
    using Unity.Properties;

    [Serializable]
    public struct BLId : IEquatable<BLId>, IComparable<BLId>
    {
        public const int ModBits = 8;
        public const int ModShift = 32 - ModBits;

        public const int MaxModsIds = 1 << ModBits;
        public const int MaxLocalIds = 1 << ModShift;
        public const int MaxLocalId = MaxLocalIds - 1;

        private const int IDMask = MaxLocalId;

        public static readonly BLId Null = default;

        [DontCreateProperty]
        public int RawValue;

        public BLId(int id, ushort mod = 0)
        {
#if UNITY_EDITOR
            if (id is < 0 or > IDMask)
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Id out of range");
            }

            if (mod >= MaxModsIds)
            {
                throw new ArgumentOutOfRangeException(nameof(mod), "Mod id too large");
            }
#endif

            RawValue = id == 0 ? 0 : mod << ModShift | id;
        }

        [CreateProperty]
        public readonly ushort Mod => (ushort)((uint)RawValue >> ModShift);

        [CreateProperty]
        public readonly int ID => RawValue & IDMask;

        public readonly bool IsNull => ID == 0;

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
            return new BLId(ID, mod);
        }

        public readonly int CompareTo(BLId other)
        {
            var modCompare = Mod.CompareTo(other.Mod);
            return modCompare != 0 ? modCompare : ID.CompareTo(other.ID);
        }

        public override readonly bool Equals(object obj)
        {
            return obj is BLId other && Equals(other);
        }

        public readonly bool Equals(BLId other)
        {
            return RawValue == other.RawValue;
        }

        public override readonly int GetHashCode()
        {
            return RawValue;
        }

        public override readonly string ToString()
        {
            return Mod == 0 ? $"ID:{ID}" : $"Mod:{Mod} ID:{ID}";
        }
    }
}
