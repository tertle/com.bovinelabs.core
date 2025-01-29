// <copyright file="NoAllocHelpersTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Utility
{
    using System.Collections.Generic;
    using BovineLabs.Core.Utility;
    using NUnit.Framework;

    /// <summary> Tests for NoAllocHelpers. </summary>
    public class NoAllocHelpersTests
    {
        /// <summary> Tests the <see cref="NoAllocHelpers.ExtractArrayFromListT{T}" /> method. </summary>
        [Test]
        public void ExtractArrayFromList()
        {
            var list = new List<int>
            {
                0,
                1,
                2,
            };

            var array = NoAllocHelpers.ExtractArrayFromList(list);

            Assert.AreEqual(0, array[0]);
            Assert.AreEqual(1, array[1]);
            Assert.AreEqual(2, array[2]);
        }

        /// <summary> Tests the <see cref="NoAllocHelpers.ResizeList{T}" /> method. </summary>
        [Test]
        public void ResizeList()
        {
            var list = new List<int>
            {
                0,
                1,
                2,
            };

            list.Capacity = 16;

            NoAllocHelpers.ResizeList(list, 16);

            Assert.AreEqual(16, list.Count);
        }
    }
}
