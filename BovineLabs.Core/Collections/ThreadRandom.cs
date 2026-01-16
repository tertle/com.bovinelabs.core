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
    using UnityEngine.Assertions;

    /// <summary> A thread safe random. As it's thread based it should not be used for anything requiring determinism. </summary>
    public unsafe struct ThreadRandom
    {
        private readonly AllocatorManager.AllocatorHandle allocator;

        [NativeDisableUnsafePtrRestriction]
        private Randoms* buffer;

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
        }

        public readonly bool IsCreated => this.buffer != null;

        public ref Random GetRandomRef()
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(JobsUtility.IsExecutingJob || UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread(),
                "Can only be used on main or worker threads");
#endif
            ref var randoms = ref UnsafeUtility.ArrayElementAsRef<Randoms>(this.buffer, JobsUtility.ThreadIndex);
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

        // 1 random per cache line to avoid false sharing
        [StructLayout(LayoutKind.Explicit, Size = JobsUtility.CacheLineSize)]
        private struct Randoms
        {
            [FieldOffset(0)]
            public Random Random;
        }
    }
}
