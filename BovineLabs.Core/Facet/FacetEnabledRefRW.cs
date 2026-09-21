namespace BovineLabs.Core
{
    using System.Threading;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary>
    /// Keep stack-local; never store this value outside the stack.
    /// </summary>
    public unsafe readonly struct FacetEnabledRefRW<T>
        where T : unmanaged, IEnableableComponent
    {
        private readonly EnabledRefRW<T> _enabled;
        private readonly bool _lookup;

        public FacetEnabledRefRW(EnabledRefRW<T> enabled, bool lookup)
        {
            _enabled = enabled;
            _lookup = lookup;
        }

        public bool IsValid => _enabled.IsValid;

        public void SetComponentEnabled(bool value)
        {
            if (!_lookup)
            {
                _enabled.ValueRW = value;
                return;
            }

            var enabledInternal = GetInternal(_enabled);
            GetBitAddress(enabledInternal, out var bits, out var mask);
            var oldBits = Interlocked.Read(ref UnsafeUtility.AsRef<long>(bits));
            long newBits;
            long expectedOldBits;

            do
            {
                newBits = math.select(oldBits & ~mask, oldBits | mask, value);
                expectedOldBits = oldBits;
                oldBits = Interlocked.CompareExchange(ref UnsafeUtility.AsRef<long>(bits), newBits, expectedOldBits);
            }
            while (expectedOldBits != oldBits);

            if (oldBits == newBits)
            {
                return;
            }

            var adjustment = math.select(1, -1, value);
            Interlocked.Add(ref UnsafeUtility.AsRef<int>(enabledInternal.PtrChunkDisabledCount), adjustment);
        }

        public bool GetComponentEnabled()
        {
            if (!_lookup)
            {
                return _enabled.ValueRO;
            }

            var enabledInternal = GetInternal(_enabled);
            GetBitAddress(enabledInternal, out var bits, out var mask);
            var value = Interlocked.Read(ref UnsafeUtility.AsRef<long>(bits));
            return (value & mask) != 0;
        }

        private static EnabledRefRWInternal GetInternal(EnabledRefRW<T> enabled)
        {
            return UnsafeUtility.As<EnabledRefRW<T>, EnabledRefRWInternal>(ref enabled);
        }

        private static void GetBitAddress(in EnabledRefRWInternal enabled, out ulong* bits, out long mask)
        {
            var wordIndex = enabled.Ptr.OffsetInBits / 64;
            bits = enabled.Ptr.Value + wordIndex;
            var indexInWord = enabled.Ptr.OffsetInBits - (wordIndex * 64);
            mask = 1L << indexInWord;
        }

        private readonly struct EnabledRefRWInternal
        {
            public readonly SafeBitRefInternal Ptr;
            public readonly int* PtrChunkDisabledCount;

            public readonly struct SafeBitRefInternal
            {
                public readonly ulong* Value;
                public readonly int OffsetInBits;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                public readonly AtomicSafetyHandle Safety;
#endif
            }
        }
    }
}
