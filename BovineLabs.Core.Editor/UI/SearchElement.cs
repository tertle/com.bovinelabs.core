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
        private readonly List<SearchView.Item> _items;
        private readonly Button _componentButton;

        public SearchElement(List<SearchView.Item> items, string defaultText, string displayName = "")
            : this(items, defaultText, displayName, new VisualElement())
        {
        }

        private SearchElement(List<SearchView.Item> items, string defaultText, string displayName, VisualElement element)
            : base(displayName, element)
        {
            AddToClassList(BaseField<string>.alignedFieldUssClassName);
            AddToClassList(TextInputBaseField<string>.ussClassName);

            element.AddToClassList("unity-base-field__input");
            element.AddToClassList("unity-base-popup-field__input");
            element.AddToClassList("unity-popup-field__input");
            element.AddToClassList("unity-property-field__input");

            _componentButton = new Button();
            element.Add(_componentButton);
            _componentButton.AddToClassList("unity-base-popup-field__text");
            _componentButton.RemoveFromClassList("unity-button");

            var image = new VisualElement();
            element.Add(image);
            image.AddToClassList("unity-base-popup-field__arrow");

            _items = items;
            labelElement.style.minWidth = 60;

            _componentButton.clicked += () =>
            {
                var searchWindow = SearchWindow.Create();

                searchWindow.Title = displayName;
                searchWindow.Items = items;
                searchWindow.OnSelection += item =>
                {
                    OnSelection?.Invoke(item);
                    _componentButton.text = SetText(item);
                };

                var rect = EditorWindow.focusedWindow.position;

                Rect worldBounds;
                if (labelElement.parent == null)
                {
                    worldBounds = element.worldBound;
                }
                else
                {
                    worldBounds = labelElement.worldBound;
                    worldBounds.width += element.worldBound.width;
                }

                var size = new Rect(rect.x + worldBounds.x, rect.y + worldBounds.y + worldBounds.height, worldBounds.width, Height);
                searchWindow.Position = size;
                searchWindow.Show();
            };

            _componentButton.text = defaultText;
        }

        public event Action<SearchView.Item> OnSelection;

        public Func<SearchView.Item, string> SetText { get; set; } = item => item.Name;

        public float Height { get; set; } = 315;

        public string Text
        {
            get => _componentButton.text;
            set => _componentButton.text = value;
        }

        public void SetValue(int index)
        {
            var item = _items[index];

            OnSelection?.Invoke(item);
            _componentButton.text = SetText(item);
        }
    }
}
