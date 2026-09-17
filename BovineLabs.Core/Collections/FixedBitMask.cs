namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Utility;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct FixedBitMask<T>
        where T : unmanaged, IFixedSize
    {
        private const int Idx = 3;
        private const int Shift = (1 << Idx) - 1;

        private T data;

        public int Length => UnsafeUtility.SizeOf<T>() << 3;

        public void Set(int pos, bool value)
        {
            this.CheckArgs(pos, 1);

            fixed (T* t = &this.data)
            {
                var ptr = (byte*)t;

                var idx = pos >> Idx;
                var shift = pos & Shift;
                var mask = (byte)(1 << shift);

                var bits = (byte)((ptr[idx] & ~mask) | (-value.AsByte() & mask));
                ptr[idx] = bits;
            }
        }

        public bool IsSet(int pos)
        {
            this.CheckArgs(pos, 1);

            fixed (T* t = &this.data)
            {
                var ptr = (byte*)t;

                var idx = pos >> Idx;
                var shift = pos & Shift;
                var mask = (byte)(1 << shift);
                return (ptr[idx] & mask) != 0;
            }
        }

        public void Reset()
        {
            fixed (T* t = &this.data)
            {
                UnsafeUtility.MemClear(t, UnsafeUtility.SizeOf<T>());
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckArgs(int pos, int numBits)
        {
            if (pos < 0 || pos >= this.Length || numBits < 1)
            {
                throw new ArgumentException($"BitArray invalid arguments: pos {pos} (must be 0-{this.Length}), numBits {numBits} (must be greater than 0).");
            }
        }
    }
}
