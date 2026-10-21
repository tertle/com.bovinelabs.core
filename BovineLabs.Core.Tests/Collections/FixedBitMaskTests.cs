namespace BovineLabs.Core.Tests.Collections
{
    using System;
    using System.Runtime.InteropServices;
    using BovineLabs.Core.Collections;
    using NUnit.Framework;

    public class FixedBitMaskTests
    {
        [Test]
        public void SetOutOfRange()
        {
            var bitMask = default(FixedBitMask<Bytes4>);
            Assert.Throws<ArgumentException>(() => bitMask.Set(-1, true));
            Assert.Throws<ArgumentException>(() => bitMask.Set(32, true));
        }

        [Test]
        public void IsSetOutOfRange()
        {
            var bitMask = default(FixedBitMask<Bytes4>);
            Assert.Throws<ArgumentException>(() => bitMask.IsSet(-1));
            Assert.Throws<ArgumentException>(() => bitMask.IsSet(32));
        }


        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct Bytes4 : IFixedSize
        {
        }
    }
}
