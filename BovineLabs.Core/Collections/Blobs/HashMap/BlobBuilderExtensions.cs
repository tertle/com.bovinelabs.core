namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using BovineLabs.Core.Internal;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    public static unsafe class BlobBuilderExtensions
    {
        // 16384 is somewhat arbitrary but tests have shown that for small enough capacities this will
        // be a bit faster while still not allocating loads of memory
        private const int UseBucketCapacityRatioOfThreeUpTo = 16384;

        public static void* Allocate(this ref BlobBuilder blobBuilder, int size)
        {
            ref var bb = ref UnsafeUtility.As<BlobBuilder, BlobBuilderInternal>(ref blobBuilder);
            var allocation = bb.Allocate(size, UnsafeUtility.AlignOf<byte>());
            return bb.AllocationToPointer(allocation);
        }

        public static T* Allocate<T>(this ref BlobBuilder blobBuilder, ref BlobPtr<T> ptr, int size)
            where T : unmanaged
        {
            ref var bb = ref UnsafeUtility.As<BlobBuilder, BlobBuilderInternal>(ref blobBuilder);

            return (T*)bb.Allocate(ref ptr, size);
        }

        public static ref T Allocate<T>(this ref BlobBuilder blobBuilder, ref BlobPtr<byte> ptr)
            where T : unmanaged
        {
            ref var typedBlob = ref UnsafeUtility.As<BlobPtr<byte>, BlobPtr<T>>(ref ptr);
            return ref blobBuilder.Allocate(ref typedBlob);
        }

        public static void Construct<T>(this ref BlobBuilder builder, ref BlobArray<T> dest, in NativeArray<T> src) where T : unmanaged
        {
            var blobArr = builder.Allocate(ref dest, src.Length);

            // bulk-copy
            var dst = UnsafeUtility.AddressOf(ref blobArr[0]);
            var srcPtr = src.GetUnsafeReadOnlyPtr();
            var  bytes  = (long)src.Length * UnsafeUtility.SizeOf<T>();

            UnsafeUtility.MemCpy(dst, srcPtr, bytes);
        }

        public static void Construct<T>(this ref BlobBuilder builder, ref BlobArray<T> dest, in NativeList<T> src) where T : unmanaged
        {
            var list = src;
            builder.Construct(ref dest, list.AsArray());
        }

        public static void ConstructHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobHashMap<TKey, TValue> blobHashMap, ref NativeParallelHashMap<TKey, TValue> source)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var count = source.Count();
            var hashMapBuilder = builder.AllocateHashMap(ref blobHashMap, count);

            using var e = source.GetEnumerator();
            while (e.MoveNext())
            {
                hashMapBuilder.Add(e.Current.Key, e.Current.Value);
            }
        }

        public static void ConstructHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobHashMap<TKey, TValue> blobHashMap, Dictionary<TKey, TValue> source)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var count = source.Count;
            var ratio = count <= UseBucketCapacityRatioOfThreeUpTo ? 3 : 2;

            var hashMapBuilder = builder.AllocateHashMap(ref blobHashMap, source.Count, ratio);
            foreach (var kv in source)
            {
                hashMapBuilder.Add(kv.Key, kv.Value);
            }
        }

        /// <summary>
        /// Capacity is fixed at allocation.
        /// </summary>
        public static BlobBuilderHashMap<TKey, TValue> AllocateHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobHashMap<TKey, TValue> blobHashMap, int capacity)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return AllocateHashMap(ref builder, ref blobHashMap, capacity, capacity <= UseBucketCapacityRatioOfThreeUpTo ? 3 : 2);
        }

        /// <summary>
        /// Capacity is fixed at allocation; increasing the bucket ratio trades more memory for fewer collisions.
        /// </summary>
        public static BlobBuilderHashMap<TKey, TValue> AllocateHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobHashMap<TKey, TValue> blobHashMap, int capacity, int bucketCapacityRatio)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var hashmapBuilder = new BlobBuilderHashMap<TKey, TValue>(capacity, bucketCapacityRatio, ref builder, ref blobHashMap.Data);

            return hashmapBuilder;
        }

        public static void ConstructMultiHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobMultiHashMap<TKey, TValue> blobMultiHashMap, ref NativeParallelMultiHashMap<TKey, TValue> source)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var count = source.Count();
            var hashMapBuilder = builder.AllocateMultiHashMap(ref blobMultiHashMap, count);

            using var e = source.GetEnumerator();
            while (e.MoveNext())
            {
                hashMapBuilder.Add(e.Current.Key, e.Current.Value);
            }
        }

        /// <summary>
        /// Capacity is fixed at allocation.
        /// </summary>
        public static BlobBuilderMultiHashMap<TKey, TValue> AllocateMultiHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobMultiHashMap<TKey, TValue> blobMultiHashMap, int capacity)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return new BlobBuilderMultiHashMap<TKey, TValue>(capacity, capacity <= UseBucketCapacityRatioOfThreeUpTo ? 3 : 2, ref builder,
                ref blobMultiHashMap.Data);
        }

        /// <summary>
        /// Capacity is fixed at allocation; increasing the bucket ratio trades more memory for fewer collisions.
        /// </summary>
        public static BlobBuilderMultiHashMap<TKey, TValue> AllocateMultiHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobMultiHashMap<TKey, TValue> blobMultiHashMap, int capacity, int bucketCapacityRatio)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return new BlobBuilderMultiHashMap<TKey, TValue>(capacity, bucketCapacityRatio, ref builder, ref blobMultiHashMap.Data);
        }

        public static BlobBuilderPerfectHashMap<TKey, TValue> ConstructPerfectHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobPerfectHashMap<TKey, TValue> blobHashMap, NativeHashMap<TKey, TValue> source, TValue nullValue = default)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged, IEquatable<TValue>
        {
            return new BlobBuilderPerfectHashMap<TKey, TValue>(ref builder, ref blobHashMap, source, nullValue);
        }

        public static IntPtr GetListPtr(this BlobBuilder builder)
        {
            ref var bb = ref UnsafeUtility.As<BlobBuilder, BlobBuilderInternal>(ref builder);
            return new IntPtr(bb.Allocations.GetListData());
        }

        public static bool ContainsAllocation(this ref BlobBuilder builder, void* address, int size)
        {
            if (address == null || size < 0)
            {
                return false;
            }

            ref var bb = ref UnsafeUtility.As<BlobBuilder, BlobBuilderInternal>(ref builder);
            var start = (byte*)address;
            var end = start + size;
            foreach (var allocation in bb.Allocations)
            {
                if (start >= allocation.P && end <= allocation.P + allocation.Size)
                {
                    return true;
                }
            }

            return false;
        }

        public static int GetFinalizedPayloadOffset(this ref BlobBuilder builder, void* address, int size)
        {
            if (address == null)
            {
                throw new ArgumentNullException(nameof(address));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size));
            }

            ref var bb = ref UnsafeUtility.As<BlobBuilder, BlobBuilderInternal>(ref builder);
            var finalizedOffset = 0;
            var start = (byte*)address;
            var end = start + size;
            foreach (var allocation in bb.Allocations)
            {
                if (start >= allocation.P && end <= allocation.P + allocation.Size)
                {
                    return checked(finalizedOffset + (int)(start - allocation.P));
                }

                finalizedOffset = checked(finalizedOffset + allocation.Size);
            }

            throw new ArgumentException("The address is outside the BlobBuilder allocation.", nameof(address));
        }

        private struct BlobBuilderInternal
        {
            public AllocatorManager.AllocatorHandle Allocator;
            public NativeList<BlobAllocation> Allocations;
            public NativeList<OffsetPtrPatch> Patches;
            public int CurrentChunkIndex;
            public int ChunkSize;

            public struct BlobAllocation
            {
                public int Size;
                public byte* P;
            }

            public struct BlobDataRef
            {
                public int AllocIndex;
                public int Offset;
            }

            public struct OffsetPtrPatch
            {
                public int* OffsetPtr;
                public BlobDataRef Target;
                public int Length; // if length != 0 this is an array patch and the length should be patched
            }

            public BlobDataRef Allocate(int size, int alignment)
            {
                if (size > ChunkSize)
                {
                    size = CollectionHelper.Align(size, 16);
                    var allocIndex = Allocations.Length;
                    var mem = (byte*)CollectionMemory.Allocate(size, alignment, Allocator);
                    UnsafeUtility.MemClear(mem, size);
                    Allocations.Add(new BlobAllocation
                    {
                        P = mem,
                        Size = size,
                    });

                    return new BlobDataRef
                    {
                        AllocIndex = allocIndex,
                        Offset = 0,
                    };
                }

                var alloc = EnsureEnoughRoomInChunk(size, alignment);

                var offset = alloc.Size;
                UnsafeUtility.MemClear(alloc.P + alloc.Size, size);
                alloc.Size += size;
                Allocations[CurrentChunkIndex] = alloc;
                return new BlobDataRef
                {
                    AllocIndex = CurrentChunkIndex,
                    Offset = offset,
                };
            }

            public void* AllocationToPointer(BlobDataRef blobDataRef)
            {
                return Allocations[blobDataRef.AllocIndex].P + blobDataRef.Offset;
            }

            public void AllocateBlobAssetReference(ref BlobBuilder target, ref BlobPtr<BlobAssetHeader> blobPtr)
            {
                // Avoid crash when there are no chunks (DOTS-8681)
                if (CurrentChunkIndex != -1)
                {
                    //Align last chunk upwards so all chunks are 16 byte aligned
                    AlignChunk(CurrentChunkIndex);
                }

                var offsets = new NativeArray<int>(Allocations.Length + 1, Unity.Collections.Allocator.Temp);
                var sortedAllocs = new NativeArray<SortedIndex>(Allocations.Length, Unity.Collections.Allocator.Temp);

                offsets[0] = 0;
                for (var i = 0; i < Allocations.Length; ++i)
                {
                    offsets[i + 1] = offsets[i] + Allocations[i].Size;
                    sortedAllocs[i] = new SortedIndex
                    {
                        P = Allocations[i].P,
                        Index = i,
                    };
                }

                var dataSize = offsets[Allocations.Length];

                sortedAllocs.Sort();
                var sortedPatches = new NativeArray<SortedIndex>(Patches.Length, Unity.Collections.Allocator.Temp);
                for (var i = 0; i < Patches.Length; ++i)
                {
                    sortedPatches[i] = new SortedIndex
                    {
                        P = (byte*)Patches[i].OffsetPtr,
                        Index = i,
                    };
                }

                sortedPatches.Sort();

                ref var bt = ref UnsafeUtility.As<BlobBuilder, BlobBuilderInternal>(ref target);
                var buffer = (byte*)bt.Allocate(ref blobPtr, sizeof(BlobAssetHeader) + dataSize);

                var data = buffer + sizeof(BlobAssetHeader);

                for (var i = 0; i < Allocations.Length; ++i)
                {
                    UnsafeUtility.MemCpy(data + offsets[i], Allocations[i].P, Allocations[i].Size);
                }

                var iAlloc = 0;
                var allocStart = Allocations[sortedAllocs[0].Index].P;
                var allocEnd = allocStart + Allocations[sortedAllocs[0].Index].Size;

                for (var i = 0; i < Patches.Length; ++i)
                {
                    var patchIndex = sortedPatches[i].Index;
                    var offsetPtr = (int*)sortedPatches[i].P;

                    while (offsetPtr >= allocEnd)
                    {
                        ++iAlloc;
                        allocStart = Allocations[sortedAllocs[iAlloc].Index].P;
                        allocEnd = allocStart + Allocations[sortedAllocs[iAlloc].Index].Size;
                    }

                    var patch = Patches[patchIndex];

                    var offsetPtrInData = offsets[sortedAllocs[iAlloc].Index] + (int)((byte*)offsetPtr - allocStart);
                    var targetPtrInData = offsets[patch.Target.AllocIndex] + patch.Target.Offset;

                    *(int*)(data + offsetPtrInData) = targetPtrInData - offsetPtrInData;
                    if (patch.Length != 0)
                    {
                        *(int*)(data + offsetPtrInData + 4) = patch.Length;
                    }
                }

                sortedPatches.Dispose();
                sortedAllocs.Dispose();
                offsets.Dispose();

                var header = (BlobAssetHeader*)buffer;
                *header = new BlobAssetHeader();
                header->Length = dataSize;
                header->Allocator = Unity.Collections.Allocator.Persistent;

                // @TODO use 64bit hash
                header->Hash = math.hash(buffer + sizeof(BlobAssetHeader), dataSize);

                header->ValidationPtr = buffer + sizeof(BlobAssetHeader);
            }

            public void* Allocate<T>(ref BlobPtr<T> ptr, int size)
                where T : unmanaged
            {
                var offsetPtr = (int*)UnsafeUtility.AddressOf(ref ptr.m_OffsetPtr);

                ValidateAllocation(offsetPtr);

                var allocation = Allocate(size, UnsafeUtility.AlignOf<T>());

                var patch = new OffsetPtrPatch
                {
                    OffsetPtr = offsetPtr,
                    Target = allocation,
                    Length = 0,
                };

                Patches.Add(patch);
                return AllocationToPointer(allocation);
            }

            private BlobAllocation EnsureEnoughRoomInChunk(int size, int alignment)
            {
                if (CurrentChunkIndex == -1)
                {
                    return AllocateNewChunk();
                }

                var alloc = Allocations[CurrentChunkIndex];
                var startOffset = CollectionHelper.Align(alloc.Size, alignment);
                if (startOffset + size > ChunkSize)
                {
                    return AllocateNewChunk();
                }

                UnsafeUtility.MemClear(alloc.P + alloc.Size, startOffset - alloc.Size);

                alloc.Size = startOffset;
                return alloc;
            }

            private BlobAllocation AllocateNewChunk()
            {
                // align size of last chunk to 16 bytes so chunks can be concatenated without breaking alignment
                if (CurrentChunkIndex != -1)
                {
                    AlignChunk(CurrentChunkIndex);
                }

                CurrentChunkIndex = Allocations.Length;
                var alloc = new BlobAllocation
                {
                    P = (byte*)CollectionMemory.Allocate(ChunkSize, 16, Allocator),
                    Size = 0,
                };

                Allocations.Add(alloc);
                return alloc;
            }

            private void AlignChunk(int chunkIndex)
            {
                var chunk = Allocations[chunkIndex];
                var oldSize = chunk.Size;
                chunk.Size = CollectionHelper.Align(chunk.Size, 16);
                Allocations[chunkIndex] = chunk;
                UnsafeUtility.MemSet(chunk.P + oldSize, 0, chunk.Size - oldSize);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidateAllocation(void* address)
            {
                // ValidateAllocation is most often called with data in recently allocated allocations
                // so this searches backwards
                for (var i = Allocations.Length - 1; i >= 0; --i)
                {
                    var allocation = Allocations[i];
                    if (address >= allocation.P && address < allocation.P + allocation.Size)
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(
                    "The BlobArray passed to Allocate was not allocated by this ref BlobBuilder " +
                    "or the struct that embeds it was copied by value instead of by ref.");
            }

            private struct SortedIndex : IComparable<SortedIndex>
            {
                public byte* P;
                public int Index;

                public int CompareTo(SortedIndex other)
                {
                    return ((ulong)P).CompareTo((ulong)other.P);
                }
            }
        }
    }
}
