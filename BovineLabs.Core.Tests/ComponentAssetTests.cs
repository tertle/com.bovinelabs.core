// <copyright file="ComponentAssetTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests
{
    using System;
    using System.Reflection;
    using NUnit.Framework;
    using Unity.Entities;
    using UnityEngine;

    public class ComponentAssetTests
    {
        private TypeAsset asset;

        [TearDown]
        public void TearDown()
        {
            if (this.asset != null)
            {
                UnityEngine.Object.DestroyImmediate(this.asset);
            }
        }

        [Test]
        public void ResolveType_WithAssignedType_ReturnsType()
        {
            var typeAsset = this.CreateAsset<TypeAsset>(typeof(ValueComponent));

            Assert.AreEqual(typeof(ValueComponent), typeAsset.ResolveType());
        }

        [Test]
        public void ResolveType_WithoutAssignedType_Throws()
        {
            var typeAsset = this.CreateAsset<TypeAsset>(null);

            Assert.Throws<InvalidOperationException>(() => typeAsset.ResolveType());
        }

        [Test]
        public void ResolveType_WithMissingType_Throws()
        {
            var typeAsset = this.CreateAsset<TypeAsset>(null);
            SetTypeName(typeAsset, "Missing.Type, Missing.Assembly");

            Assert.Throws<TypeLoadException>(() => typeAsset.ResolveType());
        }

        [Test]
        public void ComponentEnableableAsset_WithNonEnableableComponent_ThrowsOnResolve()
        {
            var componentAsset = this.CreateAsset<ComponentEnableableAsset>(typeof(ValueComponent));

            var exception = Assert.Throws<InvalidCastException>(() => componentAsset.ResolveType());
            StringAssert.Contains(nameof(ComponentEnableableAsset), exception!.Message);
            StringAssert.Contains("not enableable", exception.Message);
        }

        [Test]
        public void ComponentTagAsset_WithNonZeroSizedComponent_ThrowsOnResolve()
        {
            var componentAsset = this.CreateAsset<ComponentTagAsset>(typeof(ValueComponent));

            var exception = Assert.Throws<InvalidCastException>(() => componentAsset.ResolveType());
            StringAssert.Contains(nameof(ComponentTagAsset), exception!.Message);
            StringAssert.Contains("not zero-sized", exception.Message);
        }

        [Test]
        public void ComponentEnableableAsset_WithEnableableComponent_ReturnsStableTypeHash()
        {
            var enableableAsset = this.CreateAsset<ComponentEnableableAsset>(typeof(EnableableComponent));
            var enableableHash = TypeManager.GetTypeInfo<EnableableComponent>().StableTypeHash;

            Assert.AreEqual(enableableHash, enableableAsset.GetStableTypeHash());
        }

        [Test]
        public void ComponentTagAsset_WithZeroSizedComponent_ReturnsStableTypeHash()
        {
            var tagAsset = this.CreateAsset<ComponentTagAsset>(typeof(TagComponent));
            var tagHash = TypeManager.GetTypeInfo<TagComponent>().StableTypeHash;

            Assert.AreEqual(tagHash, tagAsset.GetStableTypeHash());
        }

        private T CreateAsset<T>(Type type)
            where T : TypeAsset
        {
            var typeAsset = ScriptableObject.CreateInstance<T>();
            this.asset = typeAsset;

            if (type != null)
            {
                SetTypeName(typeAsset, type.AssemblyQualifiedName);
            }

            return typeAsset;
        }

        private static void SetTypeName(TypeAsset typeAsset, string typeName)
        {
            var field = typeof(TypeAsset).GetField("typeName", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field);
            field.SetValue(typeAsset, typeName);
        }

        private struct ValueComponent : IComponentData
        {
            public int Value;
        }

        private struct EnableableComponent : IComponentData, IEnableableComponent
        {
        }

        private struct TagComponent : IComponentData
        {
        }
    }
}
