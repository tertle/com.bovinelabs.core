// <copyright file="FacetEnabledRefRW.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System.Threading;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary> Provides enable-bit access that is fast for chunk iteration and atomic for entity lookup. </summary>
    /// <remarks> Do not store this value outside the stack. </remarks>
    /// <typeparam name="T">The enableable component or buffer type.</typeparam>
    public unsafe readonly struct FacetEnabledRefRW<T>
        where T : unmanaged, IEnableableComponent
    {
        private readonly EnabledRefRW<T> enabled;
        private readonly bool lookup;

        /// <summary> Initializes a new instance of the <see cref="FacetEnabledRefRW{T}"/> struct. </summary>
        /// <param name="enabled">The enabled reference resolved by the facet generator.</param>
        /// <param name="lookup">Whether access came from an entity lookup and therefore requires atomic operations.</param>
        public FacetEnabledRefRW(EnabledRefRW<T> enabled, bool lookup)
        {
            this.enabled = enabled;
            this.lookup = lookup;
        }

        /// <summary> Gets a value indicating whether the enabled reference is valid. </summary>
        public bool IsValid => this.enabled.IsValid;

        /// <summary> Atomically sets lookup-backed bits and uses the direct fast path for chunk-backed bits. </summary>
        /// <param name="value">The enabled state to set.</param>
        public void SetComponentEnabled(bool value)
        {
            if (!this.lookup)
            {
                this.enabled.ValueRW = value;
                return;
            }

            var enabledInternal = GetInternal(this.enabled);
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

        /// <summary> Atomically reads lookup-backed bits and uses the direct fast path for chunk-backed bits. </summary>
        /// <returns>The current enabled state.</returns>
        public bool GetComponentEnabled()
        {
            if (!this.lookup)
            {
                return this.enabled.ValueRO;
            }

            var enabledInternal = GetInternal(this.enabled);
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
