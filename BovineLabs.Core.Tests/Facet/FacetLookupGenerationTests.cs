// <copyright file="FacetLookupGenerationTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests.Facet
{
    using System;
    using System.Reflection;
    using NUnit.Framework;

    public class FacetLookupGenerationTests
    {
        private const BindingFlags NestedTypeFlags = BindingFlags.Public | BindingFlags.NonPublic;

        [Test]
        public void EnabledRefROFacetKeepsLookup()
        {
            var nestedLookup = typeof(TestFacet).GetNestedType("Lookup", NestedTypeFlags);

            Assert.AreSame(typeof(TestFacet.Lookup), nestedLookup);
        }

        [TestCase(typeof(FacetEnabledRefRWFacet))]
        [TestCase(typeof(OptionalFacetEnabledRefRWFacet))]
        [TestCase(typeof(RequiredNestedFacetEnabledRefRWFacet))]
        [TestCase(typeof(OptionalNestedFacetEnabledRefRWFacet))]
        [TestCase(typeof(BufferFacetEnabledRefRWFacet))]
        [TestCase(typeof(FacetTest))]
        [TestCase(typeof(FaceReadonlyTest))]
        public void FacetEnabledRefRWFacetKeepsLookupAndChunkAccess(Type facetType)
        {
            Assert.IsNotNull(facetType.GetNestedType("Lookup", NestedTypeFlags));
            Assert.IsNotNull(facetType.GetNestedType("TypeHandle", NestedTypeFlags));
            Assert.IsNotNull(facetType.GetNestedType("ResolvedChunk", NestedTypeFlags));
        }

        [Test]
        public void FacetEnabledRefRWFacetUsesAccessSpecificStorage()
        {
            Assert.AreEqual(
                typeof(Unity.Entities.ComponentLookup<EnabledB>),
                GetMemberType(typeof(FacetEnabledRefRWFacet.Lookup), "EnabledBs"));
            Assert.AreEqual(
                typeof(Unity.Entities.ComponentTypeHandle<EnabledB>),
                GetMemberType(typeof(FacetEnabledRefRWFacet.TypeHandle), "EnabledBHandle"));
            Assert.AreEqual(
                typeof(Unity.Entities.EnabledMask),
                GetMemberType(typeof(FacetEnabledRefRWFacet.ResolvedChunk), "EnabledBs"));

            Assert.AreEqual(
                typeof(Unity.Entities.BufferLookup<EnabledBufferElement>),
                GetMemberType(typeof(BufferFacetEnabledRefRWFacet.Lookup), "EnabledBufferElements"));
            Assert.AreEqual(
                typeof(Unity.Entities.BufferTypeHandle<EnabledBufferElement>),
                GetMemberType(typeof(BufferFacetEnabledRefRWFacet.TypeHandle), "EnabledBufferElementHandle"));
            Assert.AreEqual(
                typeof(Unity.Entities.EnabledMask),
                GetMemberType(typeof(BufferFacetEnabledRefRWFacet.ResolvedChunk), "EnabledBufferElements"));
        }

        private static Type GetMemberType(Type declaringType, string name)
        {
            var field = declaringType.GetField(name);
            if (field != null)
            {
                return field.FieldType;
            }

            return declaringType.GetProperty(name)?.PropertyType;
        }
    }
}
