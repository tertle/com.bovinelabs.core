// <copyright file="MemoryLabelExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Memory
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class MemoryLabelExtensions
    {
        public static void* MallocTracked(this MemoryLabel memoryLabel, long size, int alignment, int callstacksToSkip)
        {
#if UNITY_6000_3_OR_NEWER
            return UnsafeUtility.MallocTracked(size, alignment, memoryLabel, callstacksToSkip + 1);
#else
            return UnsafeUtility.MallocTracked(size, alignment, memoryLabel.allocator, callstacksToSkip + 1);
#endif
        }

        public static void FreeTracked(this MemoryLabel memoryLabel, void* memory)
        {
#if UNITY_6000_3_OR_NEWER
            UnsafeUtility.FreeTracked(memory, memoryLabel);
#else
            UnsafeUtility.FreeTracked(memory, memoryLabel.allocator);
#endif
        }
    }
}
