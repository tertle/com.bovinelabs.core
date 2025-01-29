// <copyright file="UnsafeThreadStreamBlockData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Collections;
    using UnityEngine;

    [GenerateTestsForBurstCompatibility]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1649:File name should match first type name", Justification = "Convenience")]
    internal unsafe struct UnsafeThreadStreamBlock
    {
        internal UnsafeThreadStreamBlock* Next;
        internal fixed byte Data[1];
    }

    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeThreadStreamRange
    {
        internal UnsafeThreadStreamBlock* Block;
        internal int OffsetInFirstBlock;
        internal int ElementCount;

        // One byte past the end of the last byte written
        internal int LastOffset;
        internal int NumberOfBlocks;

        internal UnsafeThreadStreamBlock* CurrentBlock;
        internal byte* CurrentPtr;
        internal byte* CurrentBlockEnd;
    }

    [GenerateTestsForBurstCompatibility]
    internal unsafe struct UnsafeThreadStreamBlockData
    {
        internal const int AllocationSize = 4 * 1024;
        internal AllocatorManager.AllocatorHandle Allocator;

        internal UnsafeThreadStreamBlock** Blocks;

        internal UnsafeThreadStreamRange* Ranges;

        internal UnsafeThreadStreamBlock* Allocate(UnsafeThreadStreamBlock* oldBlock, int threadIndex)
        {
            Debug.Assert(threadIndex < UnsafeThreadStream.ForEachCount && threadIndex >= 0);

            var block = (UnsafeThreadStreamBlock*)Memory.Unmanaged.Allocate(AllocationSize, 16, this.Allocator);
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
