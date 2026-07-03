// <copyright file="ElementPropertyTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Tests
{
    using System;
    using System.Linq;
    using BovineLabs.Core.Editor.Helpers;
    using BovineLabs.Core.Editor.Inspectors;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class ElementPropertyTests
    {
        [Test]
        public void CreatePropertyGUI_UsesSerializedPropertyTooltipForFoldout()
        {
            var host = ScriptableObject.CreateInstance<ElementPropertyHost>();

            try
            {
                var serializedObject = new SerializedObject(host);
                var property = serializedObject.FindProperty(nameof(ElementPropertyHost.Value));
                var root = new TestElementProperty().CreatePropertyGUI(property);

                Assert.AreEqual(ElementPropertyHost.TooltipText, root.tooltip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GetChildren_SkipSingleRoot_UnwrapsSingleGenericChildWithChildren()
        {
            var host = ScriptableObject.CreateInstance<ElementPropertyHost>();

            try
            {
                var serializedObject = new SerializedObject(host);
                var paths = GetChildPaths(serializedObject, nameof(ElementPropertyHost.SingleRoot), true);

                CollectionAssert.AreEqual(
                    new[]
                    {
                        $"{nameof(ElementPropertyHost.SingleRoot)}.{nameof(SingleRootValue.Value)}.{nameof(NestedValue.Integer)}",
                        $"{nameof(ElementPropertyHost.SingleRoot)}.{nameof(SingleRootValue.Value)}.{nameof(NestedValue.Text)}",
                    },
                    paths);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GetChildren_SkipSingleRoot_KeepsSinglePrimitiveChild()
        {
            var host = ScriptableObject.CreateInstance<ElementPropertyHost>();

            try
            {
                var serializedObject = new SerializedObject(host);
                var paths = GetChildPaths(serializedObject, nameof(ElementPropertyHost.SinglePrimitiveRoot), true);

                CollectionAssert.AreEqual(
                    new[] { $"{nameof(ElementPropertyHost.SinglePrimitiveRoot)}.{nameof(SinglePrimitiveRootValue.Value)}" },
                    paths);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GetChildren_SkipSingleRoot_KeepsMultipleRootChildren()
        {
            var host = ScriptableObject.CreateInstance<ElementPropertyHost>();

            try
            {
                var serializedObject = new SerializedObject(host);
                var paths = GetChildPaths(serializedObject, nameof(ElementPropertyHost.MultipleRoot), true);

                CollectionAssert.AreEqual(
                    new[]
                    {
                        $"{nameof(ElementPropertyHost.MultipleRoot)}.{nameof(MultipleRootValue.First)}",
                        $"{nameof(ElementPropertyHost.MultipleRoot)}.{nameof(MultipleRootValue.Second)}",
                    },
                    paths);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void GetChildren_SkipSingleRoot_KeepsSingleArrayChild()
        {
            var host = ScriptableObject.CreateInstance<ElementPropertyHost>();

            try
            {
                var serializedObject = new SerializedObject(host);
                var paths = GetChildPaths(serializedObject, nameof(ElementPropertyHost.SingleArrayRoot), true);

                CollectionAssert.AreEqual(
                    new[] { $"{nameof(ElementPropertyHost.SingleArrayRoot)}.{nameof(SingleArrayRootValue.Value)}" },
                    paths);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private static string[] GetChildPaths(SerializedObject serializedObject, string propertyName, bool skipSingleRoot)
        {
            var property = serializedObject.FindProperty(propertyName);
            return SerializedHelper.GetChildren(property, skipSingleRoot).Select(p => p.propertyPath).ToArray();
        }

        private sealed class TestElementProperty : ElementProperty
        {
        }

        private sealed class ElementPropertyHost : ScriptableObject
        {
            public const string TooltipText = "Value tooltip";

            [Tooltip(TooltipText)]
            public ElementPropertyValue Value;

            public SingleRootValue SingleRoot = new();

            public SinglePrimitiveRootValue SinglePrimitiveRoot = new();

            public MultipleRootValue MultipleRoot = new();

            public SingleArrayRootValue SingleArrayRoot = new();
        }

        [Serializable]
        private struct ElementPropertyValue
        {
            public int Value;
        }

        [Serializable]
        private class SingleRootValue
        {
            public NestedValue Value = new();
        }

        [Serializable]
        private class SinglePrimitiveRootValue
        {
            public int Value;
        }

        [Serializable]
        private class MultipleRootValue
        {
            public NestedValue First = new();

            public NestedValue Second = new();
        }

        [Serializable]
        private class SingleArrayRootValue
        {
            public int[] Value = Array.Empty<int>();
        }

        [Serializable]
        private class NestedValue
        {
            public int Integer;

            public string Text = string.Empty;
        }
    }
}
