// <copyright file="BlobStringRefTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Collections.Blobs
{
    using System.Text;
    using BovineLabs.Core.Collections;
    using NUnit.Framework;
    using Unity.Collections;
    using Unity.Entities;

    public class BlobStringRefTests
    {
        [Test]
        public void DefaultIsEmpty()
        {
            var blobString = default(BlobStringRef);

            Assert.IsFalse(blobString.IsCreated);
            Assert.AreEqual(0, blobString.Length);
            Assert.AreEqual(string.Empty, blobString.GetString());
        }

        [TestCase("")]
        [TestCase("@UI:line")]
        [TestCase("Unicode value - caf\u00e9")]
        [TestCase("This token is intentionally longer than any small fixed string payload would be expected to hold safely.")]
        public void GetStringRoundTripsBlobString(string value)
        {
            using var blob = CreateBlob(value);
            ref var blobString = ref blob.Value.Value;
            var blobStringRef = BlobStringRef.From(ref blobString);

            Assert.IsTrue(blobStringRef.IsCreated);
            Assert.AreEqual(Encoding.UTF8.GetByteCount(value), blobStringRef.Length);
            Assert.AreEqual(value, blobStringRef.GetString());
        }

        [TestCase("")]
        [TestCase("@UI:dialogue.line")]
        public void CopyToCopiesUtf8Bytes(string value)
        {
            using var blob = CreateBlob(value);
            ref var blobString = ref blob.Value.Value;
            var blobStringRef = BlobStringRef.From(ref blobString);
            var destination = new NativeList<byte>(Allocator.Temp);

            try
            {
                var error = blobStringRef.CopyTo(ref destination);

                Assert.AreEqual(ConversionError.None, error);
                Assert.AreEqual(Encoding.UTF8.GetByteCount(value), destination.Length);
                Assert.AreEqual(value, Encoding.UTF8.GetString(destination.AsArray().ToArray()));
            }
            finally
            {
                destination.Dispose();
            }
        }

        [Test]
        public void DefaultCopyToLeavesDestinationEmpty()
        {
            var blobStringRef = default(BlobStringRef);
            var destination = new NativeList<byte>(Allocator.Temp);

            try
            {
                var error = blobStringRef.CopyTo(ref destination);

                Assert.AreEqual(ConversionError.None, error);
                Assert.AreEqual(0, destination.Length);
            }
            finally
            {
                destination.Dispose();
            }
        }

        [Test]
        public void CanCopyThroughTransientComponentPayload()
        {
            const string value = "@UI:line";
            using var blob = CreateBlob(value);
            ref var blobString = ref blob.Value.Value;
            var payload = new TestComponent
            {
                Value = BlobStringRef.From(ref blobString),
            };

            Assert.IsTrue(payload.Value.IsCreated);
            Assert.AreEqual(value, payload.Value.GetString());
        }

        private static BlobAssetReference<TestBlob> CreateBlob(string value)
        {
            var builder = new BlobBuilder(Allocator.Temp);
            ref var root = ref builder.ConstructRoot<TestBlob>();
            builder.AllocateString(ref root.Value, value);
            return builder.CreateBlobAssetReference<TestBlob>(Allocator.Persistent);
        }

        private struct TestBlob
        {
            public BlobString Value;
        }

        private struct TestComponent : IComponentData
        {
            public BlobStringRef Value;
        }
    }
}
