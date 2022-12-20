// <copyright file="SearchField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.SearchWindow
{
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class SearchField : TextField
    {
        private VisualElement searchContainer;

        public SearchField()
        {
            this.LoadLayout();
        }

        public SearchField(string label)
            : base(label)
        {
            this.LoadLayout();
        }

        public SearchField(int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : base(maxLength, multiline, isPasswordField, maskChar)
        {
            this.LoadLayout();
        }

        public SearchField(string label, int maxLength, bool multiline, bool isPasswordField, char maskChar)
            : base(label, maxLength, multiline, isPasswordField, maskChar)
        {
            this.LoadLayout();
        }

        private void LoadLayout()
        {
            this.styleSheets.Add(Resources.Load("UI/SearchFieldStyles") as StyleSheet);
            var visualTree = (VisualTreeAsset)Resources.Load("UI/SearchFieldLayout");
            visualTree.CloneTree(this);

            this.searchContainer = this.Q<VisualElement>(null, "search-field__container");

            this.RegisterCallback<FocusInEvent>(_ => { this.searchContainer.style.display = DisplayStyle.None; });

            this.RegisterCallback<FocusOutEvent>(_ => { this.searchContainer.style.display = this.value.Length == 0 ? DisplayStyle.Flex : DisplayStyle.None; });
        }

        internal new class UxmlFactory : UxmlFactory<SearchField, UxmlTraits>
        {
        }
    }
}
