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

        /// <summary>
        /// Allocates a <see cref="BlobArray{T}"/> inside <paramref name="builder"/> and copies the full contents of <paramref name="src"/> into it.
        /// </summary>
        public static void Construct<T>(this ref BlobBuilder builder, ref BlobArray<T> dest, in NativeArray<T> src) where T : unmanaged
        {
            var blobArr = builder.Allocate(ref dest, src.Length);

            // bulk-copy
            var dst = UnsafeUtility.AddressOf(ref blobArr[0]);
            var srcPtr = src.GetUnsafeReadOnlyPtr();
            var  bytes  = (long)src.Length * UnsafeUtility.SizeOf<T>();

            UnsafeUtility.MemCpy(dst, srcPtr, bytes);
        }

        /// <summary>
        /// Allocates a <see cref="BlobArray{T}"/> inside <paramref name="builder"/> and copies the full contents of <paramref name="src"/> into it.
        /// </summary>
        public static void Construct<T>(this ref BlobBuilder builder, ref BlobArray<T> dest, in NativeList<T> src) where T : unmanaged
        {
            var list = src;
            builder.Construct(ref dest, list.AsArray());
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
        /// <param name="nullValue"> The null value to compare against. </param>
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
            return new IntPtr(bb.Allocations.m_ListData);
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
                if (size > this.ChunkSize)
                {
                    size = CollectionHelper.Align(size, 16);
                    var allocIndex = this.Allocations.Length;
                    var mem = (byte*)Memory.Unmanaged.Allocate(size, alignment, this.Allocator);
                    UnsafeUtility.MemClear(mem, size);
                    this.Allocations.Add(new BlobAllocation
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

                var alloc = this.EnsureEnoughRoomInChunk(size, alignment);

                var offset = alloc.Size;
                UnsafeUtility.MemClear(alloc.P + alloc.Size, size);
                alloc.Size += size;
                this.Allocations[this.CurrentChunkIndex] = alloc;
                return new BlobDataRef
                {
                    AllocIndex = this.CurrentChunkIndex,
                    Offset = offset,
                };
            }

            public void* AllocationToPointer(BlobDataRef blobDataRef)
            {
                return this.Allocations[blobDataRef.AllocIndex].P + blobDataRef.Offset;
            }

            public void AllocateBlobAssetReference(ref BlobBuilder target, ref BlobPtr<BlobAssetHeader> blobPtr)
            {
                // Avoid crash when there are no chunks (DOTS-8681)
                if (this.CurrentChunkIndex != -1)
                {
                    //Align last chunk upwards so all chunks are 16 byte aligned
                    this.AlignChunk(this.CurrentChunkIndex);
                }

                var offsets = new NativeArray<int>(this.Allocations.Length + 1, Unity.Collections.Allocator.Temp);
                var sortedAllocs = new NativeArray<SortedIndex>(this.Allocations.Length, Unity.Collections.Allocator.Temp);

                offsets[0] = 0;
                for (var i = 0; i < this.Allocations.Length; ++i)
                {
                    offsets[i + 1] = offsets[i] + this.Allocations[i].Size;
                    sortedAllocs[i] = new SortedIndex
                    {
                        P = this.Allocations[i].P,
                        Index = i,
                    };
                }

                var dataSize = offsets[this.Allocations.Length];

                sortedAllocs.Sort();
                var sortedPatches = new NativeArray<SortedIndex>(this.Patches.Length, Unity.Collections.Allocator.Temp);
                for (var i = 0; i < this.Patches.Length; ++i)
                {
                    sortedPatches[i] = new SortedIndex
                    {
                        P = (byte*)this.Patches[i].OffsetPtr,
                        Index = i,
                    };
                }

                sortedPatches.Sort();

                ref var bt = ref UnsafeUtility.As<BlobBuilder, BlobBuilderInternal>(ref target);
                var buffer = (byte*)bt.Allocate(ref blobPtr, sizeof(BlobAssetHeader) + dataSize);

                var data = buffer + sizeof(BlobAssetHeader);

                for (var i = 0; i < this.Allocations.Length; ++i)
                {
                    UnsafeUtility.MemCpy(data + offsets[i], this.Allocations[i].P, this.Allocations[i].Size);
                }

                var iAlloc = 0;
                var allocStart = this.Allocations[sortedAllocs[0].Index].P;
                var allocEnd = allocStart + this.Allocations[sortedAllocs[0].Index].Size;

                for (var i = 0; i < this.Patches.Length; ++i)
                {
                    var patchIndex = sortedPatches[i].Index;
                    var offsetPtr = (int*)sortedPatches[i].P;

                    while (offsetPtr >= allocEnd)
                    {
                        ++iAlloc;
                        allocStart = this.Allocations[sortedAllocs[iAlloc].Index].P;
                        allocEnd = allocStart + this.Allocations[sortedAllocs[iAlloc].Index].Size;
                    }

                    var patch = this.Patches[patchIndex];

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

                this.ValidateAllocation(offsetPtr);

                var allocation = this.Allocate(size, UnsafeUtility.AlignOf<T>());

                var patch = new OffsetPtrPatch
                {
                    OffsetPtr = offsetPtr,
                    Target = allocation,
                    Length = 0,
                };

                this.Patches.Add(patch);
                return this.AllocationToPointer(allocation);
            }

            private BlobAllocation EnsureEnoughRoomInChunk(int size, int alignment)
            {
                if (this.CurrentChunkIndex == -1)
                {
                    return this.AllocateNewChunk();
                }

                var alloc = this.Allocations[this.CurrentChunkIndex];
                var startOffset = CollectionHelper.Align(alloc.Size, alignment);
                if (startOffset + size > this.ChunkSize)
                {
                    return this.AllocateNewChunk();
                }

                UnsafeUtility.MemClear(alloc.P + alloc.Size, startOffset - alloc.Size);

                alloc.Size = startOffset;
                return alloc;
            }

            private BlobAllocation AllocateNewChunk()
            {
                // align size of last chunk to 16 bytes so chunks can be concatenated without breaking alignment
                if (this.CurrentChunkIndex != -1)
                {
                    this.AlignChunk(this.CurrentChunkIndex);
                }

                this.CurrentChunkIndex = this.Allocations.Length;
                var alloc = new BlobAllocation
                {
                    P = (byte*)Memory.Unmanaged.Allocate(this.ChunkSize, 16, this.Allocator),
                    Size = 0,
                };

                this.Allocations.Add(alloc);
                return alloc;
            }

            private void AlignChunk(int chunkIndex)
            {
                var chunk = this.Allocations[chunkIndex];
                var oldSize = chunk.Size;
                chunk.Size = CollectionHelper.Align(chunk.Size, 16);
                this.Allocations[chunkIndex] = chunk;
                UnsafeUtility.MemSet(chunk.P + oldSize, 0, chunk.Size - oldSize);
            }

            [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
            [Conditional("UNITY_DOTS_DEBUG")]
            private void ValidateAllocation(void* address)
            {
                // ValidateAllocation is most often called with data in recently allocated allocations
                // so this searches backwards
                for (var i = this.Allocations.Length - 1; i >= 0; --i)
                {
                    var allocation = this.Allocations[i];
                    if (address >= allocation.P && address < allocation.P + allocation.Size)
                    {
                        return;
                    }
                }

                throw new InvalidOperationException(
                    "The BlobArray passed to Allocate was not allocated by this BlobBuilder or the struct that embeds it was copied by value instead of by ref.");
            }

            private struct SortedIndex : IComparable<SortedIndex>
            {
                public byte* P;
                public int Index;

                public int CompareTo(SortedIndex other)
                {
                    return ((ulong)this.P).CompareTo((ulong)other.P);
                }
            }
        }
    }
}
