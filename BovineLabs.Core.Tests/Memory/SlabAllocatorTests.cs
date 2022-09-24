// <copyright file="SlabAllocatorTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Memory
{
    using BovineLabs.Core.Memory;
    using NUnit.Framework;
    using Unity.Collections;

    public class SlabAllocatorTests
    {
        [Test]
        public unsafe void Alloc()
        {
            var allocator = new NativeSlabAllocator<int>(16, Allocator.Temp);

            for (var i = 0; i < 16; i++)
            {
                allocator.Alloc();
            }

            allocator.Alloc();
            allocator.Dispose();
        }
    }
}
