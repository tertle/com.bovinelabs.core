namespace BovineLabs.Core.Collections
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;

    /// <summary>
    /// Thread-local randomness is not deterministic; do not use where determinism is required.
    /// </summary>
    public unsafe struct ThreadRandom
    {
        private readonly AllocatorManager.AllocatorHandle _allocator;

        [NativeDisableUnsafePtrRestriction]
        private Randoms* _buffer;

        public ThreadRandom(uint seed, AllocatorManager.AllocatorHandle allocator)
        {
            _allocator = allocator;
            _buffer = (Randoms*)CollectionMemory.Allocate(sizeof(Randoms) * JobsUtility.ThreadIndexCount, UnsafeUtility.AlignOf<Randoms>(), allocator);

            // uint.MaxValue is invalid for Random.CreateFromIndex
            seed = (uint)math.min(seed, uint.MaxValue - JobsUtility.ThreadIndexCount - 1);

            for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                _buffer[i].Random = Random.CreateFromIndex((uint)(seed + i));
            }
        }

        public readonly bool IsCreated => _buffer != null;

        public ref Random GetRandomRef()
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(JobsUtility.IsExecutingJob || UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread(),
                "Can only be used on main or worker threads");
#endif
            ref var randoms = ref UnsafeUtility.ArrayElementAsRef<Randoms>(_buffer, JobsUtility.ThreadIndex);
            return ref randoms.Random;
        }

        public void Dispose()
        {
            if (!IsCreated)
            {
                return;
            }

            CollectionMemory.Free(_buffer, _allocator);
            _buffer = null;
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
