namespace BovineLabs.Core.Tests
{
    using System.Globalization;
    using NUnit.Framework;
    using Unity.Entities;
    using Unity.Transforms;
    using UnityEditor.Search;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public class IdentifierSearchTests
    {
        private const string EntityIdProvider = "bovinelabs-entity-id";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            TypeManager.Initialize();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void StableTypeHash_AboveDoubleIntegerPrecision_ResolvesExactComponent(bool hexadecimal)
        {
            var hash = TypeManager.GetTypeInfo<LocalTransform>().StableTypeHash;
            Assert.Greater(hash, 1UL << 53);
            var identifier = hexadecimal ? $"0x{hash:X}" : hash.ToString(CultureInfo.InvariantCulture);
            using var context = SearchService.CreateContext(TypeAsset.SearchProviderType, $"at: stabletypehash={identifier}", SearchFlags.Synchronous);
            using var results = SearchService.Request(context);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(typeof(LocalTransform).AssemblyQualifiedName, results[0].data);
        }

        [Test]
        public void StableTypeHash_AdjacentValueWithSameDoubleRepresentation_DoesNotMatch()
        {
            var hash = TypeManager.GetTypeInfo<LocalTransform>().StableTypeHash;
            Assert.Greater(hash, 1UL << 53);
            var adjacentHash = (double)(hash + 1) == (double)hash ? hash + 1 : hash - 1;
            Assert.AreEqual(TypeIndex.Null, TypeManager.GetTypeIndexFromStableTypeHash(adjacentHash));
            var identifier = adjacentHash.ToString(CultureInfo.InvariantCulture);
            using var context = SearchService.CreateContext(TypeAsset.SearchProviderType, $"at: stabletypehash={identifier}", SearchFlags.Synchronous);
            using var results = SearchService.Request(context);

            Assert.IsEmpty(results);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TypeIndex_FullOrMaskedBufferIndex_ResolvesSameComponent(bool masked)
        {
            var typeIndex = TypeManager.GetTypeIndex<Child>();
            Assert.AreNotEqual(typeIndex.Index, typeIndex.Value);
            var identifier = (masked ? typeIndex.Index : typeIndex.Value).ToString(CultureInfo.InvariantCulture);
            using var context = SearchService.CreateContext(TypeAsset.SearchProviderType, $"at: typeindex={identifier}", SearchFlags.Synchronous);
            using var results = SearchService.Request(context);

            Assert.AreEqual(1, results.Count);
            Assert.AreEqual(typeof(Child).AssemblyQualifiedName, results[0].data);
        }

    }
}
