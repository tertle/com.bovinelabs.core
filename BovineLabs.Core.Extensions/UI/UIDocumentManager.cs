// <copyright file="UIDocumentManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using Unity.Logging;
    using UnityEngine;
    using UnityEngine.UIElements;

    [RequireComponent(typeof(UIDocument))]
    public class UIDocumentManager : MonoBehaviour, IUIDocumentManager
    {
        private const string RootClassName = "root";

        private readonly List<OrderedElement> elements = new();
        private readonly List<OrderedElement> rootElements = new();

        private VisualElement? viewUnsafe;
        private VisualElement? rootUnsafe;

#if UNITY_EDITOR
        public event Action? EditorRebuild;
#endif

        public static IUIDocumentManager Instance { get; private set; } = new NullDocumentManager();

        public VisualElement Root => this.rootUnsafe!;

        private VisualElement View => this.viewUnsafe!;

        public void AddRoot(VisualElement visualElement, int priority = 0)
        {
            var e = new OrderedElement(visualElement, priority);

            this.rootElements.Add(e);
            this.rootElements.Sort();

            var index = this.rootElements.IndexOf(e);
            this.Root.Insert(index, visualElement);
        }

        public void RemoveRoot(VisualElement visualElement)
        {
            var index = this.rootElements.IndexOf(new OrderedElement(visualElement, 0));

            if (index < 0)
            {
                Debug.LogError($"Removing {visualElement} that isn't added.");
            }
            else
            {
                this.rootElements.RemoveAt(index);
                visualElement.RemoveFromHierarchy();
            }
        }

        /// <inheritdoc />
        public void AddPanel(VisualElement visualElement, int priority = 0)
        {
            var e = new OrderedElement(visualElement, priority);

            this.elements.Add(e);
            this.elements.Sort();

            var index = this.elements.IndexOf(e);
            this.View.Insert(index, visualElement);
        }

        /// <inheritdoc />
        public void RemovePanel(VisualElement visualElement)
        {
            var index = this.elements.IndexOf(new OrderedElement(visualElement, 0));

            if (index < 0)
            {
                Debug.LogError($"Removing {visualElement} that isn't added.");
            }
            else
            {
                this.elements.RemoveAt(index);
                visualElement.RemoveFromHierarchy();
            }
        }

        private void Awake()
        {
            Instance = this;

            this.viewUnsafe = new VisualElement { pickingMode = PickingMode.Ignore };
            this.View.AddToClassList(RootClassName);
        }

        private void Start()
        {
            this.AddRoot(this.View);
        }

        private void OnEnable()
        {
            var document = this.GetComponent<UIDocument>();
            this.rootUnsafe = this.GetRoot(document);
            this.Root.Add(this.View);
#if UNITY_EDITOR
            this.EditorRebuild?.Invoke();
#endif
        }

        private void OnDisable()
        {
            this.rootUnsafe?.Remove(this.View);
        }

        [SuppressMessage("ReSharper", "MemberCanBeMadeStatic.Local", Justification = "SystemAPI requirement")]
        private VisualElement GetRoot(UIDocument document)
        {
            const string rootName = "Root";

            if (document.rootVisualElement == null)
            {
                Log.Error($"{nameof(UIDocument)} root not found.");
                return new VisualElement();
            }

            var documentRoot = document.rootVisualElement.Q(rootName);
            if (documentRoot == null)
            {
                Log.Error($"{rootName} not found.");
                return new VisualElement();
            }

            return documentRoot;
        }

        private readonly struct OrderedElement : IComparable<OrderedElement>, IEquatable<OrderedElement>
        {
            private readonly VisualElement element;
            private readonly int priority;

            public OrderedElement(VisualElement visualElement, int priority)
            {
                this.priority = priority;
                this.element = visualElement;
            }

            public VisualElement Element => this.element;

            /// <inheritdoc />
            public int CompareTo(OrderedElement other)
            {
                return this.priority.CompareTo(other.priority);
            }

            /// <inheritdoc />
            public bool Equals(OrderedElement other)
            {
                return this.element.Equals(other.element);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                return this.element.GetHashCode();
            }
        }

        private class NullDocumentManager : IUIDocumentManager
        {
            private VisualElement? root;

#if UNITY_EDITOR
#pragma warning disable CS0067
            public event Action? EditorRebuild;
#pragma warning restore CS0067
#endif

            public VisualElement Root => this.root ??= new VisualElement();

            public void AddRoot(VisualElement visualElement, int priority = 0)
            {
            }

            public void RemoveRoot(VisualElement visualElement)
            {
            }

            public void AddPanel(VisualElement visualElement, int priority = 0)
            {
            }

            public void RemovePanel(VisualElement visualElement)
            {
            }
        }
    }
}
