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
        private UnsafeThreadStreamBlockData* blockData;

        private AllocatorManager.AllocatorHandle allocator;

        public UnsafeThreadStream(Allocator allocator)
        {
            AllocateBlock(out this, allocator);
            this.AllocateForEach();
        }

        public static int ForEachCount => JobsUtility.ThreadIndexCount;

        public bool IsCreated => this.blockData != null;

        public bool IsEmpty()
        {
            if (!this.IsCreated)
            {
                return true;
            }

            for (var i = 0; i != ForEachCount; i++)
            {
                if (this.blockData->Ranges[i].ElementCount > 0)
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
                itemCount += this.blockData->Ranges[i].ElementCount;
            }

            return itemCount;
        }

        public NativeArray<T> ToNativeArray<T>(Allocator arrayAllocator)
            where T : unmanaged
        {
            var array = new NativeArray<T>(this.Count(), arrayAllocator, NativeArrayOptions.UninitializedMemory);
            var reader = this.AsReader();

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
            this.Deallocate();
        }

        public bool Equals(UnsafeThreadStream other)
        {
            return this.blockData == other.blockData;
        }

        public JobHandle Dispose(JobHandle inputDeps)
        {
            var jobHandle = new DisposeJob { Container = this }.Schedule(inputDeps);
            this.blockData = null;
            return jobHandle;
        }

        internal static void AllocateBlock(out UnsafeThreadStream stream, AllocatorManager.AllocatorHandle allocator)
        {
            var allocationSize = sizeof(UnsafeThreadStreamBlockData) + (sizeof(UnsafeThreadStreamBlock*) * ForEachCount);
            var buffer = (byte*)CollectionMemory.Allocate(allocationSize, 16, allocator);
            UnsafeUtility.MemClear(buffer, allocationSize);

            var block = (UnsafeThreadStreamBlockData*)buffer;

            stream.blockData = block;
            stream.allocator = allocator;

            block->Allocator = allocator;
            block->Blocks = (UnsafeThreadStreamBlock**)(buffer + sizeof(UnsafeThreadStreamBlockData));

            block->Ranges = null;
        }

        internal void AllocateForEach()
        {
            long allocationSize = sizeof(UnsafeThreadStreamRange) * ForEachCount;
            this.blockData->Ranges = (UnsafeThreadStreamRange*)CollectionMemory.Allocate(allocationSize, 16, this.allocator);
            UnsafeUtility.MemClear(this.blockData->Ranges, allocationSize);
        }

        private void Deallocate()
        {
            if (this.blockData == null)
            {
                return;
            }

            for (var i = 0; i != ForEachCount; i++)
            {
                var block = this.blockData->Blocks[i];
                while (block != null)
                {
                    var next = block->Next;
                    CollectionMemory.Free(block, this.allocator);
                    block = next;
                }
            }

            CollectionMemory.Free(this.blockData->Ranges, this.allocator);
            CollectionMemory.Free(this.blockData, this.allocator);
            this.blockData = null;
            this.allocator = Allocator.None;
        }

        [BurstCompile]
        private struct DisposeJob : IJob
        {
            public UnsafeThreadStream Container;

            public void Execute()
            {
                this.Container.Deallocate();
            }
        }
    }
}
