// <copyright file="MemoryUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections;

    public static class MemoryUtil
    {
        public static MemoryLabel CreateLabel(FixedString32Bytes category, FixedString64Bytes name, Allocator allocator = Allocator.Persistent)
        {
            return Memory.CreateLabel(category, name, allocator);
        }
    }
}
