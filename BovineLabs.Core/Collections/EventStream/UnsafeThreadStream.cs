namespace BovineLabs.Core.Collections
{
    using System;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs;
    using Unity.Jobs.LowLevel.Unsafe;

    /// <summary>
    /// Supports parallel readers and parallel writers without thread-safety checks.
    /// </summary>
    public unsafe partial struct UnsafeThreadStream : INativeDisposable, IEquatable<UnsafeThreadStream>
    {
        private static readonly int MaxLargeSize = UnsafeThreadStreamBlockData.AllocationSize - sizeof(void*);

        [NativeDisableUnsafePtrRestriction]
        private UnsafeThreadStreamBlockData* _blockData;

        private AllocatorManager.AllocatorHandle _allocator;

        public UnsafeThreadStream(Allocator allocator)
        {
            AllocateBlock(out this, allocator);
            AllocateForEach();
        }

        public static int ForEachCount => JobsUtility.ThreadIndexCount;

        public bool IsCreated => _blockData != null;

        public bool IsEmpty()
        {
            if (!IsCreated)
            {
                return true;
            }

            for (var i = 0; i != ForEachCount; i++)
            {
                if (_blockData->Ranges[i].ElementCount > 0)
                {
                    return false;
                }
            }

            return true;
        }

        public Reader AsReader()
        {
            return new Reader(ref this);
        }

        public Writer AsWriter()
        {
            return new Writer(ref this);
        }

        public int Count()
        {
            var itemCount = 0;

            for (var i = 0; i != ForEachCount; i++)
            {
                itemCount += _blockData->Ranges[i].ElementCount;
            }

            return itemCount;
        }

        public NativeArray<T> ToNativeArray<T>(Allocator arrayAllocator)
            where T : unmanaged
        {
            var array = new NativeArray<T>(Count(), arrayAllocator, NativeArrayOptions.UninitializedMemory);
            var reader = AsReader();

            var offset = 0;
            for (var i = 0; i != reader.ForEachCount; i++)
            {
                reader.BeginForEachIndex(i);
                var rangeItemCount = reader.RemainingItemCount;
                for (var j = 0; j < rangeItemCount; ++j)
                {
                    array[offset] = reader.Read<T>();
                    offset++;
                }

                reader.EndForEachIndex();
            }

            return array;
        }

        public void Dispose()
        {
            Deallocate();
        }

        public bool Equals(UnsafeThreadStream other)
        {
            return _blockData == other._blockData;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new DisposeJob { Container = this }.Schedule(inputDeps);
            _blockData = null;
            return jobHandle;
        }

        internal static void AllocateBlock(out UnsafeThreadStream stream, AllocatorManager.AllocatorHandle allocator)
        {
            var allocationSize = sizeof(UnsafeThreadStreamBlockData) + (sizeof(UnsafeThreadStreamBlock*) * ForEachCount);
            var buffer = (byte*)CollectionMemory.Allocate(allocationSize, 16, allocator);
            UnsafeUtility.MemClear(buffer, allocationSize);

            var block = (UnsafeThreadStreamBlockData*)buffer;

            stream._blockData = block;
            stream._allocator = allocator;

            block->Allocator = allocator;
            block->Blocks = (UnsafeThreadStreamBlock**)(buffer + sizeof(UnsafeThreadStreamBlockData));

            block->Ranges = null;
        }

        internal void AllocateForEach()
        {
            long allocationSize = sizeof(UnsafeThreadStreamRange) * ForEachCount;
            _blockData->Ranges = (UnsafeThreadStreamRange*)CollectionMemory.Allocate(allocationSize, 16, _allocator);
            UnsafeUtility.MemClear(_blockData->Ranges, allocationSize);
        }

        private void Deallocate()
        {
            if (_blockData == null)
            {
                return;
            }

            for (var i = 0; i != ForEachCount; i++)
            {
                var block = _blockData->Blocks[i];
                while (block != null)
                {
                    var next = block->Next;
                    CollectionMemory.Free(block, _allocator);
                    block = next;
                }
            }

            CollectionMemory.Free(_blockData->Ranges, _allocator);
            CollectionMemory.Free(_blockData, _allocator);
            _blockData = null;
            _allocator = Allocator.None;
        }

        [BurstCompile]
        private struct DisposeJob : IJob
        {
            public UnsafeThreadStream Container;

            public void Execute()
            {
                Container.Deallocate();
            }
        }
    }
}
