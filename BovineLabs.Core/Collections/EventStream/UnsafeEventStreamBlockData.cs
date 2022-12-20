// <copyright file="UnsafeEventStreamBlockData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Collections;
    using UnityEngine;

    [GenerateTestsForBurstCompatibility]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Convenience")]
    internal unsafe struct UnsafeEventStreamBlock
    {
        internal UnsafeEventStreamBlock* Next;
        internal fixed byte Data[1];
    }

    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeEventStreamRange
    {
        internal UnsafeEventStreamBlock* Block;
        internal int OffsetInFirstBlock;
        internal int ElementCount;

        // One byte past the end of the last byte written
        internal int LastOffset;
        internal int NumberOfBlocks;

        internal UnsafeEventStreamBlock* CurrentBlock;
        internal byte* CurrentPtr;
        internal byte* CurrentBlockEnd;
    }

    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeEventStreamBlockData
    {
        internal const int AllocationSize = 4 * 1024;
        internal AllocatorManager.AllocatorHandle Allocator;

        internal UnsafeEventStreamBlock** Blocks;

        internal UnsafeEventStreamRange* Ranges;

        internal UnsafeEventStreamBlock* Allocate(UnsafeEventStreamBlock* oldBlock, int threadIndex)
        {
            Debug.Assert((threadIndex < UnsafeEventStream.ForEachCount) && (threadIndex >= 0));

            var block = (UnsafeEventStreamBlock*)Memory.Unmanaged.Allocate(AllocationSize, 16, this.Allocator);
            block->Next = null;

            if (oldBlock == null)
            {
                // Append our new block in front of the previous head.
                block->Next = this.Blocks[threadIndex];
                this.Blocks[threadIndex] = block;
            }
            else
            {
                block->Next = oldBlock->Next;
                oldBlock->Next = block;
            }

            return block;
        }
    }
}
