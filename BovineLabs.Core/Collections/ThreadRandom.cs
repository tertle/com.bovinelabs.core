// <copyright file="ThreadRandom.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.InteropServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary> A thread safe random. As it's thread based it should not be used for anything requiring determinism. </summary>
    public unsafe struct ThreadRandom
    {
        private readonly AllocatorManager.AllocatorHandle allocator;

        [NativeDisableUnsafePtrRestriction]
        private Randoms* buffer;

        [NativeSetThreadIndex]
        private int threadIndex;

        public ThreadRandom(uint seed, AllocatorManager.AllocatorHandle allocator)
        {
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
            ref var randoms = ref UnsafeUtility.ArrayElementAsRef<Randoms>(this.buffer, this.threadIndex);
            return ref randoms.Random;
        }

        public void Dispose()
        {
            if (!this.IsCreated)
            {
                return;
            }

            Memory.Unmanaged.Free(this.buffer, this.allocator);
            this.buffer = null;
        }

        [StructLayout(LayoutKind.Explicit, Size = JobsUtility.CacheLineSize)]
        private struct Randoms
        {
            [FieldOffset(0)]
            public Random Random;
        }
    }
}
