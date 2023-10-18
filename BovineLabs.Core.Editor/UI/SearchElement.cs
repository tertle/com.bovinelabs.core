// <copyright file="SearchElement.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.SearchWindow;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    public class SearchElement : BaseField<int>
    {
        private readonly List<SearchView.Item> items;
        private readonly Button componentButton;

        public SearchElement(List<SearchView.Item> items, string defaultText, string displayName = "")
            : this(items, defaultText, displayName, new VisualElement())
        {
        }

        private SearchElement(List<SearchView.Item> items, string defaultText, string displayName, VisualElement element)
            : base(displayName, element)
        {
            this.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            this.AddToClassList(TextInputBaseField<string>.ussClassName);

            element.AddToClassList("unity-base-field__input");
            element.AddToClassList("unity-base-popup-field__input");
            element.AddToClassList("unity-popup-field__input");
            element.AddToClassList("unity-property-field__input");

            this.componentButton = new Button();
            element.Add(this.componentButton);
            this.componentButton.AddToClassList("unity-base-popup-field__text");
            this.componentButton.RemoveFromClassList("unity-button");

            var image = new VisualElement();
            element.Add(image);
            image.AddToClassList("unity-base-popup-field__arrow");

            this.items = items;
            this.labelElement.style.minWidth = 60;

            this.componentButton.clicked += () =>
            {
                var searchWindow = SearchWindow.Create();

                searchWindow.Title = displayName;
                searchWindow.Items = items;
                searchWindow.OnSelection += item =>
                {
                    this.OnSelection?.Invoke(item);
                    this.componentButton.text = this.SetText(item);
                };

                var rect = EditorWindow.focusedWindow.position;

                Rect worldBounds;
                if (this.labelElement.parent == null)
                {
                    worldBounds = element.worldBound;
                }
                else
                {
                    worldBounds = this.labelElement.worldBound;
                    worldBounds.width += element.worldBound.width;
                }

                var size = new Rect(rect.x + worldBounds.x, rect.y + worldBounds.y + worldBounds.height, worldBounds.width, this.Height);
                searchWindow.position = size;
                searchWindow.ShowPopup();
            };

            this.componentButton.text = defaultText;
        }

        public event Action<SearchView.Item>? OnSelection;

        public Func<SearchView.Item, string> SetText { get; set; } = item => item.Name;

        public float Height { get; set; } = 315;

        public string Text
        {
            get => this.componentButton.text;
            set => this.componentButton.text = value;
        }

        public void SetValue(int index)
        {
            var item = this.items[index];

            this.OnSelection?.Invoke(item);
            this.componentButton.text = this.SetText(item);
        }
    }
}
