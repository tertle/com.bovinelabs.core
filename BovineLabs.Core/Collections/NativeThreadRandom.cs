// <copyright file="NativeThreadRandom.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary> A thread safe random. As it's thread based it should not be used for anything requiring determinism. </summary>
    [NativeContainer]
    [NativeContainerIsAtomicWriteOnly]
    public unsafe struct NativeThreadRandom
    {
        private readonly AllocatorManager.AllocatorHandle allocator;

        [NativeDisableUnsafePtrRestriction]
        private Randoms* buffer;

        [NativeSetThreadIndex]
        private int threadIndex;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeThreadRandom>();
        private AtomicSafetyHandle m_Safety;
#endif

        public NativeThreadRandom(uint seed, AllocatorManager.AllocatorHandle allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            this.m_Safety = CollectionHelper.CreateSafetyHandle(allocator);
            CollectionHelper.SetStaticSafetyId<NativeThreadRandom>(ref this.m_Safety, ref s_staticSafetyId.Data);
#endif

            this.allocator = allocator;
            this.buffer = (Randoms*)Memory.Unmanaged.Allocate(sizeof(Randoms) * JobsUtility.ThreadIndexCount, UnsafeUtility.AlignOf<Randoms>(), allocator);

            // uint.MaxValue is invalid for Random.CreateFromIndex
            seed = (uint)math.min(seed, uint.MaxValue - JobsUtility.ThreadIndexCount - 1);

            for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                this.buffer[i].Random = Random.CreateFromIndex((uint)(seed + i));
            }

            this.threadIndex = 0;
        }

        public readonly bool IsCreated => this.buffer != null;

        public ref Random GetRandomRef()
        {
            this.CheckWrite();
            ref var randoms = ref UnsafeUtility.ArrayElementAsRef<Randoms>(this.buffer, this.threadIndex);
            return ref randoms.Random;
        }

        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (!AtomicSafetyHandle.IsDefaultValue(this.m_Safety))
            {
                AtomicSafetyHandle.CheckExistsAndThrow(this.m_Safety);
            }
#endif
            if (!this.IsCreated)
            {
                return;
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            CollectionHelper.DisposeSafetyHandle(ref this.m_Safety);
#endif
            Memory.Unmanaged.Free(this.buffer, this.allocator);
            this.buffer = null;
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckWrite()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(this.m_Safety);
#endif
        }

        [StructLayout(LayoutKind.Explicit, Size = JobsUtility.CacheLineSize)]
        private struct Randoms
        {
            [FieldOffset(0)]
            public Random Random;
        }
    }
}
