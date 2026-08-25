// <copyright file="MemoryLabelAllocator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using System;
    using System.Threading;
    using AOT;
    using BovineLabs.Core.Assertions;
    using BovineLabs.Core.Internal;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;
    using Unity.Mathematics;
    using UnityEngine.Assertions;

    /// <summary>
    /// Custom allocator that routes allocations to <see cref="Allocator.Persistent"/> while attaching a specific <see cref="MemoryLabel"/> for tracking.
    /// </summary>
    [BurstCompile]
    public unsafe struct MemoryLabelAllocator : AllocatorManager.IAllocator
    {
        private const long MaximumAllocationBytes = 1L << 40;

        private AllocatorManager.AllocatorHandle handle;

        private MemoryLabel memoryLabel;
        private int allocationCount;

        /// <inheritdoc />
        public AllocatorManager.TryFunction Function => Try;

        /// <inheritdoc />
        public AllocatorManager.AllocatorHandle Handle
        {
            get => this.handle;
            set => this.handle = value;
        }

        /// <inheritdoc />
        public Allocator ToAllocator => this.handle.ToAllocator;

        /// <inheritdoc />
        public bool IsCustomAllocator => this.handle.IsCustomAllocator;

        /// <inheritdoc />
        public bool IsAutoDispose => false;

        /// <summary>
        /// Initialize the allocator by constructing a label for <see cref="Allocator.Persistent"/>.
        /// </summary>
        /// <param name="category">Category associated with the label.</param>
        /// <param name="name">Object name associated with the label.</param>
        public void Initialize(FixedString32Bytes category, FixedString64Bytes name)
        {
            var label = MemoryUtil.CreateLabel(category, name);
            this.memoryLabel = label;
        }

        /// <inheritdoc />
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var remaining = Volatile.Read(ref this.allocationCount);
            if (remaining > 0)
            {
                BLGlobalLogger.LogError512($"MemoryLabelAllocator disposed with {remaining} outstanding allocations.");
            }
#endif
            this.handle.Dispose();
        }

        /// <inheritdoc />
        public int Try(ref AllocatorManager.Block block)
        {
            Check.Assume(this.memoryLabel.IsCreated);

            if (block.Range.Pointer == IntPtr.Zero)
            {
                return this.Allocate(ref block);
            }

            if (block.Bytes == 0)
            {
                return this.Free(ref block);
            }

            return this.Reallocate(ref block);
        }

        [BurstCompile(CompileSynchronously = true)]
        [MonoPInvokeCallback(typeof(AllocatorManager.TryFunction))]
        private static int Try(IntPtr state, ref AllocatorManager.Block block)
        {
            return ((MemoryLabelAllocator*)state)->Try(ref block);
        }

        private static bool TryGetSize(int bytesPerItem, int items, out long bytes)
        {
            bytes = 0;
            if (bytesPerItem < 0 || items < 0)
            {
                return false;
            }

            bytes = (long)bytesPerItem * items;

            return bytes is >= 0 and <= MaximumAllocationBytes;
        }

        private static int EnsureAlignment(ref AllocatorManager.Block block)
        {
            var alignment = math.max(JobsUtility.CacheLineSize, block.Alignment);
            block.Alignment = alignment;
            return alignment;
        }

        private int Allocate(ref AllocatorManager.Block block)
        {
            if (!TryGetSize(block.BytesPerItem, block.Range.Items, out var bytes))
            {
                return AllocatorManager.kErrorBufferOverflow;
            }

            if (bytes == 0)
            {
                block.Range.Pointer = IntPtr.Zero;
                block.AllocatedItems = 0;
                return AllocatorManager.kErrorNone;
            }

            var alignment = EnsureAlignment(ref block);
            var pointer = UnsafeUtility.MallocTracked(bytes, alignment, this.memoryLabel, 0);

            if (pointer == null)
            {
                return AllocatorManager.kErrorBufferOverflow;
            }

            block.Range.Pointer = (IntPtr)pointer;
            block.AllocatedItems = block.Range.Items;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var allocated = Interlocked.Increment(ref this.allocationCount);
            Assert.IsTrue(allocated > 0, "MemoryLabelAllocator allocation count overflowed.");
#else
            Interlocked.Increment(ref this.allocationCount);
#endif
            return AllocatorManager.kErrorNone;
        }

        private int Free(ref AllocatorManager.Block block)
        {
            var pointer = (void*)block.Range.Pointer;
            if (pointer != null)
            {
                UnsafeUtility.FreeTracked(pointer, this.memoryLabel);

                block.Range.Pointer = IntPtr.Zero;
                block.AllocatedItems = 0;
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                var remaining = Interlocked.Decrement(ref this.allocationCount);
                Assert.IsTrue(remaining >= 0, "MemoryLabelAllocator allocation count went negative.");
#else
                Interlocked.Decrement(ref this.allocationCount);
#endif
            }

            return AllocatorManager.kErrorNone;
        }

        private int Reallocate(ref AllocatorManager.Block block)
        {
            if (!TryGetSize(block.BytesPerItem, block.Range.Items, out var bytes))
            {
                return AllocatorManager.kErrorBufferOverflow;
            }

            if (bytes == 0)
            {
                return this.Free(ref block);
            }

            var alignment = EnsureAlignment(ref block);
            var newPointer = UnsafeUtility.MallocTracked(bytes, alignment, this.memoryLabel, 0);

            if (newPointer == null)
            {
                return AllocatorManager.kErrorBufferOverflow;
            }

            var existingPointer = (void*)block.Range.Pointer;
            if (existingPointer != null)
            {
                var existingBytes = (long)block.BytesPerItem * block.AllocatedItems;
                var copyBytes = existingBytes < bytes ? existingBytes : bytes;
                if (copyBytes > 0)
                {
                    UnsafeUtility.MemCpy(newPointer, existingPointer, copyBytes);
                }

                UnsafeUtility.FreeTracked(existingPointer, this.memoryLabel);
            }

            block.Range.Pointer = (IntPtr)newPointer;
            block.AllocatedItems = block.Range.Items;

            return AllocatorManager.kErrorNone;
        }
    }
}
