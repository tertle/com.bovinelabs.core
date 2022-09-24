// <copyright file="PoolAllocatorTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Memory
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Memory;
    using NUnit.Framework;
    using Unity.Collections;

    public class PoolAllocatorTests
    {
        [Test]
        public unsafe void Alloc()
        {
            var allocator = new UnsafeFixedPoolAllocator<int>(16, Allocator.Temp);

            var list = new List<IntPtr>();

            for (var i = 0; i < 16; i++)
            {
                list.Add(new IntPtr(allocator.Alloc()));
            }

            Assert.IsTrue(allocator.Alloc() == null);

            foreach (var i in list)
            {
                allocator.Free((int*)i.ToPointer());
            }

            for (var i = 0; i < 16; i++)
            {
                list.Add(new IntPtr(allocator.Alloc()));
            }

            allocator.Dispose();
        }
    }
}
