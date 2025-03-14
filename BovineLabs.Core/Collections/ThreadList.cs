// <copyright file="ThreadList.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Runtime.InteropServices;
    using Unity.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    public unsafe struct ThreadList
    {
        private readonly AllocatorManager.AllocatorHandle allocator;

        [NativeDisableUnsafePtrRestriction]
        private Lists* buffer;

        public ThreadList(AllocatorManager.AllocatorHandle allocator)
        {
            this.allocator = allocator;
            this.buffer = (Lists*)Memory.Unmanaged.Allocate(sizeof(Lists) * JobsUtility.ThreadIndexCount, UnsafeUtility.AlignOf<Lists>(), allocator);

            for (var i = 0; i < JobsUtility.ThreadIndexCount; i++)
            {
                this.buffer[i].List = new UnsafeList<byte>(512, allocator);
            }
        }

        public readonly bool IsCreated => this.buffer != null;

        public ref UnsafeList<byte> GetList()
        {
            return ref this.GetList(JobsUtility.ThreadIndex);
        }

        public ref UnsafeList<byte> GetList(int threadIndex)
        {
#if UNITY_EDITOR
            Assert.IsTrue(JobsUtility.IsExecutingJob || UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread());
            Assert.IsTrue(threadIndex >= 0 && threadIndex < JobsUtility.ThreadIndexCount);
#endif
            ref var randoms = ref UnsafeUtility.ArrayElementAsRef<Lists>(this.buffer, threadIndex);
            return ref randoms.List;
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
        private struct Lists
        {
            [FieldOffset(0)]
            public UnsafeList<byte> List;
        }
    }
}
