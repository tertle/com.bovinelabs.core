// <copyright file="UnsafeThreadStream.Writer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Collections
{
    using System.Diagnostics.CodeAnalysis;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Jobs.LowLevel.Unsafe;

    public unsafe partial struct UnsafeThreadStream
    {
        /// <summary> The writer instance. </summary>
        public readonly struct Writer
        {
            [NativeDisableUnsafePtrRestriction]
            private readonly UnsafeThreadStreamBlockData* blockStream;

            internal Writer(ref UnsafeThreadStream stream)
            {
                this.blockStream = stream.blockData;
            }

            /// <summary> Write data. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <param name="value"> Value to write. </param>
            [GenerateTestsForBurstCompatibility]
            public readonly void Write<T>(T value)
                where T : struct
            {
                ref var dst = ref this.Allocate<T>();
                dst = value;
            }

            /// <summary> Allocate space for data. </summary>
            /// <typeparam name="T"> The type of value. </typeparam>
            /// <returns> Reference to allocated space for data. </returns>
            [GenerateTestsForBurstCompatibility]
            public readonly ref T Allocate<T>()
                where T : struct
            {
                var size = UnsafeUtility.SizeOf<T>();
                return ref UnsafeUtility.AsRef<T>(this.Allocate(size));
            }

            /// <summary> Allocate space for data. </summary>
            /// <param name="size"> Size in bytes. </param>
            /// <returns> Pointer to allocated space for data. </returns>
            public readonly byte* Allocate(int size)
            {
                var threadIndex = AssumeThreadRange(JobsUtility.ThreadIndex);

                var ranges = this.blockStream->Ranges + threadIndex;

                var ptr = ranges->CurrentPtr;
                var allocationEnd = ptr + size;

                ranges->CurrentPtr = allocationEnd;

                if (allocationEnd > ranges->CurrentBlockEnd)
                {
                    var oldBlock = ranges->CurrentBlock;
                    var newBlock = this.blockStream->Allocate(oldBlock, threadIndex);

                    ranges->CurrentBlock = newBlock;
                    ranges->CurrentPtr = newBlock->Data;

                    if (ranges->Block == null)
                    {
                        ranges->OffsetInFirstBlock = (int)(newBlock->Data - (byte*)newBlock);
                        ranges->Block = newBlock;
                    }
                    else
                    {
                        ranges->NumberOfBlocks++;
                    }

                    ranges->CurrentBlockEnd = (byte*)newBlock + UnsafeThreadStreamBlockData.AllocationSize;

                    ptr = newBlock->Data;
                    ranges->CurrentPtr = newBlock->Data + size;
                }

                ranges->ElementCount++;
                ranges->LastOffset = (int)(ranges->CurrentPtr - (byte*)ranges->CurrentBlock);

                return ptr;
            }

            [return: AssumeRange(0, JobsUtility.MaxJobThreadCount - 1)]
            private static int AssumeThreadRange(int value)
            {
                return value;
            }
        }
    }
}
