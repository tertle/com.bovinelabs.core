// <copyright file="NativeLinearCongruentialGenerator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Diagnostics;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary>
    /// Linear Congruential Generator
    /// </summary>
    /// <remarks>
    /// This implements Turbo Pascal lcg.
    /// https://en.wikipedia.org/wiki/Linear_congruential_generator#c_%E2%89%A0_0
    /// </remarks>
    [NativeContainer]
    [BurstCompatible(GenericTypeArguments = new[] { typeof(int) })]
    public unsafe struct NativeLinearCongruentialGenerator : IDisposable
    {
        private const int Multiplier = 134775813;
        private const int Increment = 1;
        private const int Modulus = int.MaxValue;

        [NativeDisableUnsafePtrRestriction]
        private int* current;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private AtomicSafetyHandle m_Safety;
        private static readonly SharedStatic<int> s_SafetyId = SharedStatic<int>.GetOrCreate<NativeLinearCongruentialGenerator>();

#if REMOVE_DISPOSE_SENTINEL
#else
        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif
#endif

        private AllocatorManager.AllocatorHandle allocatorLabel;

        public NativeLinearCongruentialGenerator(int seed, Allocator allocator)
        {
            Allocate(allocator, out this);
            *this.current = seed;
        }

        private static void Allocate(AllocatorManager.AllocatorHandle allocator, out NativeLinearCongruentialGenerator reference)
        {
            CollectionHelper.CheckAllocator(allocator);

            reference = default;
            reference.current = (int*)Memory.Unmanaged.Allocate(UnsafeUtility.SizeOf<int>(), UnsafeUtility.AlignOf<int>(), allocator);
            reference.allocatorLabel = allocator;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
#if REMOVE_DISPOSE_SENTINEL
            reference.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
#else
            if (allocator.IsCustomAllocator)
            {
                reference.m_Safety = AtomicSafetyHandle.Create();
                reference.m_DisposeSentinel = null;
            }
            else
            {
                DisposeSentinel.Create(out reference.m_Safety, out reference.m_DisposeSentinel, 1, allocator.ToAllocator);
            }
#endif

            CollectionHelper.SetStaticSafetyId<NativeLinearCongruentialGenerator>(ref reference.m_Safety, ref s_SafetyId.Data);
#endif
        }

        /// <summary>
        /// Releases all resources (memory and safety handles).
        /// </summary>
        public void Dispose()
        {
            this.CheckNotDisposed();

            if (CollectionHelper.ShouldDeallocate(this.allocatorLabel))
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS

#if REMOVE_DISPOSE_SENTINEL
                CollectionHelper.DisposeSafetyHandle(ref m_Safety);
#else
                DisposeSentinel.Dispose(ref this.m_Safety, ref this.m_DisposeSentinel);
#endif
#endif
                Memory.Unmanaged.Free(this.current, this.allocatorLabel);
                this.allocatorLabel = Allocator.Invalid;
            }

            this.current = null;
        }

        public int Next()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif

            var x = *this.current;
            var x1 = ((Multiplier * x) + Increment) & Modulus;
            *this.current = x1;

            return *this.current;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private void CheckNotDisposed()
        {
            if (this.current == null)
            {
                throw new ObjectDisposedException("The NativeLinearCongruentialGenerator is already disposed.");
            }
        }
    }
}
