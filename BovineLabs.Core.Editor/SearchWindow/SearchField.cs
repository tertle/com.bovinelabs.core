// <copyright file="SearchField.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#nullable disable
namespace BovineLabs.Core.Editor.SearchWindow
{
    using BovineLabs.Core.Editor.UI;
    using UnityEngine.UIElements;

    [UxmlElement]
    internal partial class SearchField : TextField
    {
        private static readonly UITemplate SearchFieldTemplate = new(SearchWindow.RootUIPath + "SearchField");

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
            SearchFieldTemplate.Clone(this);

            this.searchContainer = this.Q<VisualElement>(null, "search-field__container");

            this.RegisterCallback<FocusInEvent>(_ => this.searchContainer.style.display = DisplayStyle.None);
            this.RegisterCallback<FocusOutEvent>(_ => this.searchContainer.style.display = this.value.Length == 0 ? DisplayStyle.Flex : DisplayStyle.None);
        }
    }
}
