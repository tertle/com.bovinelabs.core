// <copyright file="FixedBitMaskTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections
{
    using System;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using NUnit.Framework;

    public class FixedBitMaskTests
    {
        [Test]
        public void SetOutOfRange([Values(int.MinValue, -1, 32, int.MaxValue)] int pos)
        {
            var bitMask = default(FixedBitMask<Bytes4>);
            Assert.Throws<ArgumentException>(() => bitMask.Set(pos, true));
        }

        [Test]
        public void IsSetOutOfRange([Values(int.MinValue, -1, 32, int.MaxValue)] int pos)
        {
            var bitMask = default(FixedBitMask<Bytes4>);
            Assert.Throws<ArgumentException>(() => bitMask.IsSet(pos));
        }

        [Test]
        public void SetIsSetUnset([Values(0, 10, 31)] int pos)
        {
            var bitMask = default(FixedBitMask<Bytes4>);

            bitMask.Set(pos, true);
            Assert.IsTrue(bitMask.IsSet(pos));

            bitMask.Set(pos, false);
            Assert.IsFalse(bitMask.IsSet(pos));
        }

        [Test]
        public void Reset()
        {
            var bitMask = default(FixedBitMask<Bytes4>);

            bitMask.Set(1, true);
            bitMask.Set(31, true);

            bitMask.Reset();

            Assert.IsFalse(bitMask.IsSet(1));
            Assert.IsFalse(bitMask.IsSet(31));
        }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct Bytes4 : IFixedSize
        {
        }
    }
}
