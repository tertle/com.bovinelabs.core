// <copyright file="GraphToolkitElementPropertyTests.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_6000_5_OR_NEWER
namespace BovineLabs.Core.Tests
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.Inspectors;
    using NUnit.Framework;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.TestTools;
    using UnityEngine.UIElements;

    public class GraphToolkitElementPropertyTests
    {
        [Test]
        public void AlignForGraphToolkit_AddsGraphToolkitClassesToBaseFieldLabelAndInput()
        {
            var field = new IntegerField("Value");

            TestGraphToolkitElementProperty.Align(field);

            var label = field.Q<Label>(className: BaseField<int>.labelUssClassName);
            var input = field.Q<VisualElement>(className: BaseField<int>.inputUssClassName);

            Assert.IsNotNull(label);
            Assert.IsNotNull(input);
            Assert.IsTrue(label.ClassListContains(GraphToolkitElementProperty.GraphToolkitLabelUssClassName));
            Assert.IsTrue(input.ClassListContains(GraphToolkitElementProperty.GraphToolkitInputUssClassName));
        }

        [Test]
        public void AlignForGraphToolkit_AddsUnityPropertyFieldClassToOwningModelPropertyField()
        {
            var owner = new VisualElement();
            owner.AddToClassList(GraphToolkitElementProperty.GraphToolkitModelPropertyFieldUssClassName);

            var field = new IntegerField("Value");
            owner.Add(field);

            TestGraphToolkitElementProperty.Align(field);

            Assert.IsTrue(owner.ClassListContains(PropertyField.ussClassName));
        }

        [UnityTest]
        public IEnumerator AlignForGraphToolkit_UsesWidestGraphToolkitLabelInScope()
        {
            var window = ScriptableObject.CreateInstance<EditorWindow>();
            var scope = new VisualElement();
            scope.AddToClassList(GraphToolkitElementProperty.GraphToolkitInspectorFieldsUssClassName);

            var valueField = new IntegerField("Value");
            var subtitleField = new TextField("Subtitle");
            scope.Add(valueField);
            scope.Add(subtitleField);

            try
            {
                window.Show();
                window.rootVisualElement.Add(scope);

                TestGraphToolkitElementProperty.Align(valueField);
                TestGraphToolkitElementProperty.Align(subtitleField);

                yield return null;

                var valueLabel = valueField.Q<Label>(className: BaseField<int>.labelUssClassName);
                var subtitleLabel = subtitleField.Q<Label>(className: BaseField<string>.labelUssClassName);

                Assert.IsNotNull(valueLabel);
                Assert.IsNotNull(subtitleLabel);
                Assert.AreEqual(subtitleLabel.style.minWidth.value.value, valueLabel.style.minWidth.value.value);
                Assert.Greater(valueLabel.style.minWidth.value.value, 0);
            }
            finally
            {
                window.Close();
            }
        }

        [Test]
        public void CreatePropertyGUI_UsesPropertyFieldsForChildProperties()
        {
            var host = ScriptableObject.CreateInstance<GraphToolkitElementPropertyHost>();

            try
            {
                var serializedObject = new SerializedObject(host);
                var property = serializedObject.FindProperty(nameof(GraphToolkitElementPropertyHost.Value));
                var root = new TestGraphToolkitElementProperty().CreatePropertyGUI(property);
                var propertyFields = new List<PropertyField>();

                root.Query<PropertyField>().ForEach(propertyFields.Add);

                Assert.AreEqual(1, propertyFields.Count);
                Assert.IsTrue(propertyFields[0].ClassListContains(GraphToolkitElementProperty.FieldUssClassName));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void CreatePropertyGUI_UsesGraphToolkitWrapperOwnerMetadataForFoldout()
        {
            var host = ScriptableObject.CreateInstance<Wrapper_GraphToolkitElementPropertyValue_1>();

            try
            {
                var serializedObject = new SerializedObject(host);
                var property = serializedObject.FindProperty(nameof(Wrapper_GraphToolkitElementPropertyValue_1.Value));
                var root = new TestFoldoutGraphToolkitElementProperty().CreatePropertyGUI(property);

                Assert.AreEqual(GraphToolkitElementPropertyOwner.TitleText, ((Foldout)root).text);
                Assert.AreEqual(GraphToolkitElementPropertyOwner.TooltipText, root.tooltip);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        [Test]
        public void CreatePropertyGUI_IgnoresNonWrapperOwnerTitleForFoldoutText()
        {
            var host = ScriptableObject.CreateInstance<GraphToolkitElementPropertyOwnerHost>();

            try
            {
                var serializedObject = new SerializedObject(host);
                var property = serializedObject.FindProperty(nameof(GraphToolkitElementPropertyOwnerHost.Value));
                var root = new TestFoldoutGraphToolkitElementProperty().CreatePropertyGUI(property);

                Assert.AreEqual("Value", ((Foldout)root).text);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(host);
            }
        }

        private sealed class TestGraphToolkitElementProperty : GraphToolkitElementProperty
        {
            public static T Align<T>(T element)
                where T : VisualElement
            {
                return AlignForGraphToolkit(element);
            }
        }

        private sealed class TestFoldoutGraphToolkitElementProperty : GraphToolkitElementProperty
        {
            protected override bool UseFoldout => true;
        }

        private sealed class GraphToolkitElementPropertyHost : ScriptableObject
        {
            public GraphToolkitElementPropertyValue Value;
        }

        private sealed class Wrapper_GraphToolkitElementPropertyValue_1 : ScriptableObject
        {
            public GraphToolkitElementPropertyOwner Owner { get; } = new();

            public GraphToolkitElementPropertyValue Value;
        }

        private sealed class GraphToolkitElementPropertyOwnerHost : ScriptableObject
        {
            public GraphToolkitElementPropertyOwner Owner { get; } = new();

            public GraphToolkitElementPropertyValue Value;
        }

        private sealed class GraphToolkitElementPropertyOwner
        {
            public const string TitleText = "Owner Title";
            public const string TooltipText = "Owner tooltip";

            public string Title => TitleText;

            public string Tooltip => TooltipText;
        }

        [Serializable]
        private struct GraphToolkitElementPropertyValue
        {
            public int Value;
        }
    }
}
#endif
