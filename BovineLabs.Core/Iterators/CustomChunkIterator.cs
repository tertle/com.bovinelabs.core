// <copyright file="CustomChunkIterator.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System.Runtime.CompilerServices;
    using Unity.Burst.Intrinsics;
    using Unity.Entities;
    using Unity.Mathematics;

    public interface ICustomChunkIterator
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void Execute(int entityIndexInChunk);
    }

    public struct CustomChunkIterator<T>
        where T : unmanaged, ICustomChunkIterator
    {
        private readonly T execute;

        public CustomChunkIterator(T execute)
        {
            this.execute = execute;
        }

        public void Execute(in ArchetypeChunk chunk, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var chunkEntityCount = chunk.Count;
            var executor = this.execute;

            if (!useEnabledMask)
            {
                for (int entityIndexInChunk = 0; entityIndexInChunk < chunkEntityCount; ++entityIndexInChunk)
                {
                    executor.Execute(entityIndexInChunk);
                }
            }
            else
            {
                int edgeCount = math.countbits(chunkEnabledMask.ULong0 ^ (chunkEnabledMask.ULong0 << 1)) +
                    math.countbits(chunkEnabledMask.ULong1 ^ (chunkEnabledMask.ULong1 << 1)) - 1;

                bool useRanges = edgeCount <= 4;
                if (useRanges)
                {
                    int chunkEndIndex = 0;
                    while (EnabledBitUtility.TryGetNextRange(chunkEnabledMask, chunkEndIndex, out var entityIndexInChunk, out chunkEndIndex))
                    {
                        while (entityIndexInChunk < chunkEndIndex)
                        {
                            executor.Execute(entityIndexInChunk);
                            entityIndexInChunk++;
                        }
                    }
                }
                else
                {
                    ulong mask64 = chunkEnabledMask.ULong0;
                    int count = math.min(64, chunkEntityCount);
                    for (int entityIndexInChunk = 0; entityIndexInChunk < count; ++entityIndexInChunk)
                    {
                        if ((mask64 & 1) != 0)
                        {
                            executor.Execute(entityIndexInChunk);
                        }

                        mask64 >>= 1;
                    }

                    mask64 = chunkEnabledMask.ULong1;
                    for (var entityIndexInChunk = 64; entityIndexInChunk < chunkEntityCount; ++entityIndexInChunk)
                    {
                        if ((mask64 & 1) != 0)
                        {
                            executor.Execute(entityIndexInChunk);
                        }

                        mask64 >>= 1;
                    }
                }
            }
        }
    }
}
