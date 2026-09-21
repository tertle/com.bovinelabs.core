namespace BovineLabs.Core.Collections
{
    using System;
    using BovineLabs.Core.Internal;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// Implements Turbo Pascal LCG: https://en.wikipedia.org/wiki/Linear_congruential_generator#c_%E2%89%A0_0
    /// </summary>
    [NativeContainer]
    public unsafe struct NativeLinearCongruentialGenerator : IDisposable
    {
        private const int Multiplier = 134775813;
        private const int Increment = 1;
        private const int Modulus = int.MaxValue;

        [NativeDisableUnsafePtrRestriction]
        private int* _current;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve Unity safety-handle field names.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve Unity safety-handle field names.")]
        private AtomicSafetyHandle m_Safety;
        [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Preserve the established static safety ID name.")]
        [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Preserve the established static safety ID name.")]
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeLinearCongruentialGenerator>();
#endif

        private AllocatorManager.AllocatorHandle _allocatorLabel;

        public NativeLinearCongruentialGenerator(int seed, Allocator allocator)
        {
            Allocate(allocator, out this);
            *_current = seed;
        }

        private static void Allocate(AllocatorManager.AllocatorHandle allocator, out NativeLinearCongruentialGenerator reference)
        {
            CollectionChecks.CheckAllocator(allocator);

            reference = default;
            reference._current = (int*)CollectionMemory.Allocate(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), allocator);
            reference._allocatorLabel = allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            reference.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);

            CollectionHelper.SetStaticSafetyId<NativeLinearCongruentialGenerator>(ref reference.m_Safety, ref s_staticSafetyId.Data);
            AtomicSafetyHandle.SetBumpSecondaryVersionOnScheduleWrite(reference.m_Safety, true);
#endif
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#endif
            CollectionMemory.Free(_current, _allocatorLabel);

            _current = null;
        }

        public int Next()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            var x = *_current;
            var x1 = ((Multiplier * x) + Increment) & Modulus;
            *_current = x1;

            return *_current;
        }
    }
}
