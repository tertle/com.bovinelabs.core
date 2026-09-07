// <copyright file="IdentifierSearchTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

        [TestCase("stabletypehash=not-a-number")]
        [TestCase("stabletypehash=18446744073709551616")]
        [TestCase("stabletypehash=0x10000000000000000")]
        [TestCase("typeindex=not-a-number")]
        [TestCase("typeindex=2147483648")]
        [TestCase("component=true AND (")]
        public void Types_InvalidQuery_ReturnsNoResults(string query)
        {
            using var context = SearchService.CreateContext(TypeAsset.SearchProviderType, $"at: {query}", SearchFlags.Synchronous);
            using var results = SearchService.Request(context);

            Assert.IsEmpty(results);
        }

        [TestCase("decimal")]
        [TestCase("hexadecimal")]
        [TestCase("pair")]
        public void EntityId_SupportedFormat_ResolvesExactObject(string format)
        {
            var asset = ScriptableObject.CreateInstance<TypeAsset>();
            try
            {
                var entityId = asset.GetEntityId();
                var rawId = EntityId.ToULong(entityId);
                var identifier = format switch
                {
                    "decimal" => rawId.ToString(CultureInfo.InvariantCulture),
                    "hexadecimal" => $"0x{rawId:X}",
                    _ => entityId.ToString(),
                };

                using var context = SearchService.CreateContext(EntityIdProvider, $"eid: {identifier}", SearchFlags.Synchronous);
                using var results = SearchService.Request(context);

                Assert.AreEqual(1, results.Count);
                Assert.AreSame(asset, results[0].ToObject());
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void EntityId_DestroyedObject_ReturnsNoResults()
        {
            var asset = ScriptableObject.CreateInstance<TypeAsset>();
            var identifier = EntityId.ToULong(asset.GetEntityId()).ToString(CultureInfo.InvariantCulture);
            Object.DestroyImmediate(asset);
            using var context = SearchService.CreateContext(EntityIdProvider, $"eid: {identifier}", SearchFlags.Synchronous);
            using var results = SearchService.Request(context);

            Assert.IsEmpty(results);
        }

        [TestCase("not-an-id")]
        [TestCase("18446744073709551616")]
        [TestCase("0x10000000000000000")]
        [TestCase("2147483648:1")]
        [TestCase("1:2147483648")]
        [TestCase("1:2:3")]
        public void EntityId_InvalidOrOverflowingIdentifier_ReturnsNoResults(string identifier)
        {
            using var context = SearchService.CreateContext(EntityIdProvider, $"eid: {identifier}", SearchFlags.Synchronous);
            using var results = SearchService.Request(context);

            Assert.IsEmpty(results);
        }
    }
}
