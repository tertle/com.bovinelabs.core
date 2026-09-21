namespace BovineLabs.Core.Collections
{
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Internal;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    public unsafe struct ThreadList
    {
        private readonly AllocatorManager.AllocatorHandle _allocator;

        [NativeDisableUnsafePtrRestriction]
        private Lists* _buffer;

        public ThreadList(AllocatorManager.AllocatorHandle allocator)
        {
            _allocator = allocator;
            _buffer = (Lists*)CollectionMemory.Allocate(sizeof(Lists) * JobsUtility.ThreadIndexCount, UnsafeUtility.AlignOf<Lists>(), allocator);

            for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                _buffer[i].List = new UnsafeList<byte>(512, allocator);
            }
        }

        public readonly bool IsCreated => _buffer != null;

        public ref UnsafeList<byte> GetList()
        {
            return ref GetList(JobsUtility.ThreadIndex);
        }

        public ref UnsafeList<byte> GetList(int threadIndex)
        {
#if UNITY_EDITOR
            Assert.IsTrue(JobsUtility.IsExecutingJob || UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread());
            Assert.IsTrue(threadIndex >= 0 && threadIndex < JobsUtility.ThreadIndexCount);
#endif
            ref var randoms = ref UnsafeUtility.ArrayElementAsRef<Lists>(_buffer, threadIndex);
            return ref randoms.List;
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

        [StructLayout(LayoutKind.Explicit, Size = JobsUtility.CacheLineSize)]
        private struct Lists
        {
            [FieldOffset(0)]
            public UnsafeList<byte> List;
        }
    }
}
