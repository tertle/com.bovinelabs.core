// <copyright file="BlobBuilderExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Mathematics;

    /// <summary> Extension methods for BlobBuilder to allocate BlobHashMaps with. </summary>
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

        internal static void AllocateBlobAssetReference(this ref BlobBuilder blobBuilder, ref BlobPtr<BlobAssetHeader> header, BlobBuilder target)
        {
            ref var bt = ref UnsafeUtility.As<BlobBuilder, BlobBuilderInternal>(ref target);
            bt.AllocateBlobAssetReference(ref blobBuilder, ref header);
        }

        /// <summary> Allocates a BlobHashMap and copies all key value pairs from the source NativeHashMap. </summary>
        /// <param name="builder"> Reference to the struct BlobBuilder used to construct the hashmap </param>
        /// <param name="blobHashMap"> Reference to the struct BlobHashMap field </param>
        /// <param name="source"> Source hashmap to copy keys and values from </param>
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

        /// <summary> Allocates a BlobHashMap and copies all key value pairs from the source dictionary. </summary>
        /// <param name="builder"> Reference to the struct BlobBuilder used to construct the hashmap </param>
        /// <param name="blobHashMap"> Reference to the struct BlobHashMap field </param>
        /// <param name="source"> Source hashmap to copy keys and values from </param>
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

        /// <summary> Allocates a BlobHashMap and returns a builder than can be used to add values manually. </summary>
        /// <param name="builder"> Reference to the struct BlobBuilder used to construct the hashmap </param>
        /// <param name="blobHashMap"> Reference to the struct BlobHashMap field </param>
        /// <param name="capacity"> Capacity of the allocated hashmap. This value cannot be changed after allocation </param>
        /// <returns> Builder that can be ued to add values to the hashmap </returns>
        public static BlobBuilderHashMap<TKey, TValue> AllocateHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobHashMap<TKey, TValue> blobHashMap, int capacity)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return AllocateHashMap(ref builder, ref blobHashMap, capacity, capacity <= UseBucketCapacityRatioOfThreeUpTo ? 3 : 2);
        }

        /// <summary> Allocates a BlobHashMap and returns a builder than can be used to add values manually. </summary>
        /// <param name="builder"> Reference to the struct BlobBuilder used to construct the hashmap </param>
        /// <param name="blobHashMap"> Reference to the struct BlobHashMap field </param>
        /// <param name="capacity"> Capacity of the allocated hashmap. This value cannot be changed after allocation </param>
        /// <param name="bucketCapacityRatio">
        /// Bucket capacity ratio to use when allocating the hashmap.
        /// A higher value may result in less collisions and slightly better performance, but memory consumption increases exponentially.
        /// </param>
        /// <returns> Builder that can be ued to add values to the hashmap </returns>
        public static BlobBuilderHashMap<TKey, TValue> AllocateHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobHashMap<TKey, TValue> blobHashMap, int capacity, int bucketCapacityRatio)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            var hashmapBuilder = new BlobBuilderHashMap<TKey, TValue>(capacity, bucketCapacityRatio, ref builder, ref blobHashMap.Data);

            return hashmapBuilder;
        }

        /// <summary> Allocates a BlobHashMap and copies all key value pairs from the source NativeHashMap. </summary>
        /// <param name="builder"> Reference to the struct BlobBuilder used to construct the hashmap </param>
        /// <param name="blobMultiHashMap"> Reference to the struct BlobMultiHashMap field </param>
        /// <param name="source"> Source multihashmap to copy keys and values from </param>
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

        /// <summary> Allocates a BlobMultiHashMap and returns a builder than can be used to add values manually. </summary>
        /// <param name="builder"> Reference to the struct BlobBuilder used to construct the hashmap. </param>
        /// <param name="blobMultiHashMap"> Reference to the struct BlobHashMap field </param>
        /// <param name="capacity"> Capacity of the allocated multihashmap. This value cannot be changed after allocation </param>
        /// <returns> Builder that can be ued to add values to the multihashmap. </returns>
        public static BlobBuilderMultiHashMap<TKey, TValue> AllocateMultiHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobMultiHashMap<TKey, TValue> blobMultiHashMap, int capacity)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return new BlobBuilderMultiHashMap<TKey, TValue>(capacity, capacity <= UseBucketCapacityRatioOfThreeUpTo ? 3 : 2, ref builder,
                ref blobMultiHashMap.Data);
        }

        /// <summary> Allocates a BlobMultiHashMap and returns a builder than can be used to add values manually. </summary>
        /// <param name="builder"> Reference to the struct BlobBuilder used to construct the hashmap </param>
        /// <param name="blobMultiHashMap"> Reference to the struct BlobHashMap field </param>
        /// <param name="capacity"> Capacity of the allocated multihashmap. This value cannot be changed after allocation </param>
        /// <param name="bucketCapacityRatio">
        /// Bucket capacity ratio to use when allocating the hashmap.
        /// A higher value may result in less collisions and slightly better performance, but memory consumption increases exponentially.
        /// </param>
        /// <returns> Builder that can be ued to add values to the multihashmap </returns>
        public static BlobBuilderMultiHashMap<TKey, TValue> AllocateMultiHashMap<TKey, TValue>(
            this ref BlobBuilder builder, ref BlobMultiHashMap<TKey, TValue> blobMultiHashMap, int capacity, int bucketCapacityRatio)
            where TKey : unmanaged, IEquatable<TKey>
            where TValue : unmanaged
        {
            return new BlobBuilderMultiHashMap<TKey, TValue>(capacity, bucketCapacityRatio, ref builder, ref blobMultiHashMap.Data);
        }

        /// <summary> Allocates a PerfectBlobHashMap and copies all key value pairs from the source dictionary. </summary>
        /// <param name="builder"> Reference to the struct BlobBuilder used to construct the hashmap </param>
        /// <param name="blobHashMap"> Reference to the struct BlobHashMap field </param>
        /// <param name="source"> Source hashmap to copy keys and values from </param>
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
            return new IntPtr(bb.m_allocations.m_ListData);
        }

        private struct BlobBuilderInternal
        {
            public AllocatorManager.AllocatorHandle m_allocator;
            public NativeList<BlobAllocation> m_allocations;
            public NativeList<OffsetPtrPatch> m_patches;
            public int m_currentChunkIndex;
            public int m_chunkSize;

            public struct BlobAllocation
            {
                public int size;
                public byte* p;
            }

            public struct BlobDataRef
            {
                public int allocIndex;
                public int offset;
            }

            public struct OffsetPtrPatch
            {
                public int* offsetPtr;
                public BlobDataRef target;
                public int length; // if length != 0 this is an array patch and the length should be patched
            }

            public BlobDataRef Allocate(int size, int alignment)
            {
                if (size > this.m_chunkSize)
                {
                    size = CollectionHelper.Align(size, 16);
                    var allocIndex = this.m_allocations.Length;
                    var mem = (byte*)Memory.Unmanaged.Allocate(size, alignment, this.m_allocator);
                    UnsafeUtility.MemClear(mem, size);
                    this.m_allocations.Add(new BlobAllocation
                    {
                        p = mem,
                        size = size,
                    });

                    return new BlobDataRef
                    {
                        allocIndex = allocIndex,
                        offset = 0,
                    };
                }

                var alloc = this.EnsureEnoughRoomInChunk(size, alignment);

                var offset = alloc.size;
                UnsafeUtility.MemClear(alloc.p + alloc.size, size);
                alloc.size += size;
                this.m_allocations[this.m_currentChunkIndex] = alloc;
                return new BlobDataRef
                {
                    allocIndex = this.m_currentChunkIndex,
                    offset = offset,
                };
            }

            public void* AllocationToPointer(BlobDataRef blobDataRef)
            {
                return this.m_allocations[blobDataRef.allocIndex].p + blobDataRef.offset;
            }

            public void AllocateBlobAssetReference(ref BlobBuilder target, ref BlobPtr<BlobAssetHeader> blobPtr)
            {
                // Avoid crash when there are no chunks (DOTS-8681)
                if (this.m_currentChunkIndex != -1)
                {
                    //Align last chunk upwards so all chunks are 16 byte aligned
                    this.AlignChunk(this.m_currentChunkIndex);
                }

                var offsets = new NativeArray<int>(this.m_allocations.Length + 1, Allocator.Temp);
                var sortedAllocs = new NativeArray<SortedIndex>(this.m_allocations.Length, Allocator.Temp);

                offsets[0] = 0;
                for (var i = 0; i < this.m_allocations.Length; ++i)
                {
                    offsets[i + 1] = offsets[i] + this.m_allocations[i].size;
                    sortedAllocs[i] = new SortedIndex
                    {
                        p = this.m_allocations[i].p,
                        index = i,
                    };
                }

                var dataSize = offsets[this.m_allocations.Length];

                sortedAllocs.Sort();
                var sortedPatches = new NativeArray<SortedIndex>(this.m_patches.Length, Allocator.Temp);
                for (var i = 0; i < this.m_patches.Length; ++i)
                {
                    sortedPatches[i] = new SortedIndex
                    {
                        p = (byte*)this.m_patches[i].offsetPtr,
                        index = i,
                    };
                }

                sortedPatches.Sort();

                ref var bt = ref UnsafeUtility.As<BlobBuilder, BlobBuilderInternal>(ref target);
                var buffer = (byte*)bt.Allocate(ref blobPtr, sizeof(BlobAssetHeader) + dataSize);

                var data = buffer + sizeof(BlobAssetHeader);

                for (var i = 0; i < this.m_allocations.Length; ++i)
                {
                    UnsafeUtility.MemCpy(data + offsets[i], this.m_allocations[i].p, this.m_allocations[i].size);
                }

                var iAlloc = 0;
                var allocStart = this.m_allocations[sortedAllocs[0].index].p;
                var allocEnd = allocStart + this.m_allocations[sortedAllocs[0].index].size;

                for (var i = 0; i < this.m_patches.Length; ++i)
                {
                    var patchIndex = sortedPatches[i].index;
                    var offsetPtr = (int*)sortedPatches[i].p;

                    while (offsetPtr >= allocEnd)
                    {
                        ++iAlloc;
                        allocStart = this.m_allocations[sortedAllocs[iAlloc].index].p;
                        allocEnd = allocStart + this.m_allocations[sortedAllocs[iAlloc].index].size;
                    }

                    var patch = this.m_patches[patchIndex];

                    var offsetPtrInData = offsets[sortedAllocs[iAlloc].index] + (int)((byte*)offsetPtr - allocStart);
                    var targetPtrInData = offsets[patch.target.allocIndex] + patch.target.offset;

                    *(int*)(data + offsetPtrInData) = targetPtrInData - offsetPtrInData;
                    if (patch.length != 0)
                    {
                        *(int*)(data + offsetPtrInData + 4) = patch.length;
                    }
                }

                sortedPatches.Dispose();
                sortedAllocs.Dispose();
                offsets.Dispose();

                var header = (BlobAssetHeader*)buffer;
                *header = new BlobAssetHeader();
                header->Length = dataSize;
                header->Allocator = Allocator.Persistent;

                // @TODO use 64bit hash
                header->Hash = math.hash(buffer + sizeof(BlobAssetHeader), dataSize);

                header->ValidationPtr = buffer + sizeof(BlobAssetHeader);
            }

            public void* Allocate<T>(ref BlobPtr<T> ptr, int size)
                where T : unmanaged
            {
                var offsetPtr = (int*)UnsafeUtility.AddressOf(ref ptr.m_OffsetPtr);

                this.ValidateAllocation(offsetPtr);

                var allocation = this.Allocate(size, UnsafeUtility.AlignOf<T>());

                var patch = new OffsetPtrPatch
                {
                    offsetPtr = offsetPtr,
                    target = allocation,
                    length = 0,
                };

                this.m_patches.Add(patch);
                return this.AllocationToPointer(allocation);
            }

            private BlobAllocation EnsureEnoughRoomInChunk(int size, int alignment)
            {
                if (this.m_currentChunkIndex == -1)
                {
                    return this.AllocateNewChunk();
                }

                var alloc = this.m_allocations[this.m_currentChunkIndex];
                var startOffset = CollectionHelper.Align(alloc.size, alignment);
                if (startOffset + size > this.m_chunkSize)
                {
                    return this.AllocateNewChunk();
                }

                UnsafeUtility.MemClear(alloc.p + alloc.size, startOffset - alloc.size);

                alloc.size = startOffset;
                return alloc;
            }

            private BlobAllocation AllocateNewChunk()
            {
                // align size of last chunk to 16 bytes so chunks can be concatenated without breaking alignment
                if (this.m_currentChunkIndex != -1)
                {
                    this.AlignChunk(this.m_currentChunkIndex);
                }

                this.m_currentChunkIndex = this.m_allocations.Length;
                var alloc = new BlobAllocation
                {
                    p = (byte*)Memory.Unmanaged.Allocate(this.m_chunkSize, 16, this.m_allocator),
                    size = 0,
                };

                this.m_allocations.Add(alloc);
                return alloc;
            }

            private void AlignChunk(int chunkIndex)
            {
                var chunk = this.m_allocations[chunkIndex];
                var oldSize = chunk.size;
                chunk.size = CollectionHelper.Align(chunk.size, 16);
                this.m_allocations[chunkIndex] = chunk;
                UnsafeUtility.MemSet(chunk.p + oldSize, 0, chunk.size - oldSize);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidateAllocation(void* address)
            {
                // ValidateAllocation is most often called with data in recently allocated allocations
                // so this searches backwards
                for (var i = this.m_allocations.Length - 1; i >= 0; --i)
                {
                    var allocation = this.m_allocations[i];
                    if (address >= allocation.p && address < allocation.p + allocation.size)
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(
                    "The BlobArray passed to Allocate was not allocated by this BlobBuilder or the struct that embeds it was copied by value instead of by ref.");
            }

            private struct SortedIndex : IComparable<SortedIndex>
            {
                public byte* p;
                public int index;

                public int CompareTo(SortedIndex other)
                {
                    return ((ulong)this.p).CompareTo((ulong)other.p);
                }
            }
        }
    }
}
