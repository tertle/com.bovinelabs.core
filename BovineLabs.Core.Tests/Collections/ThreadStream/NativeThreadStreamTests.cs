namespace BovineLabs.Core.Tests.Collections.ThreadStream
{
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Collections;

    internal partial class NativeThreadStreamTests
    {
        [Test]
        public void CreateAndDestroy()
        {
            var stream = new NativeThreadStream(Allocator.TempJob);

            Assert.IsTrue(stream.IsCreated);
            Assert.IsTrue(stream.Count() == 0);

            stream.Dispose();
            Assert.IsFalse(stream.IsCreated);
        }
    }
}
