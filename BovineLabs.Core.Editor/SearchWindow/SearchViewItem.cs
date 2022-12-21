// <copyright file="SearchViewItem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.SearchWindow
{
    using UnityEngine;
    using UnityEngine.UIElements;
    using UnityEngine.UIExtras;

    internal class SearchViewItem : VisualElement
    {
        private readonly VisualElement icon;
        private readonly Label label;
        private readonly VisualElement nextIcon;

        public SearchViewItem()
        {
            this.AddToClassList("SearchItem");
            this.styleSheets.Add(Resources.Load<StyleSheet>("UI/SearchItemStyle"));
            var visualTree = Resources.Load<VisualTreeAsset>("UI/SearchItem");
            visualTree.CloneTree(this);

            this.label = this.Q<Label>("Label");
            this.icon = this.Q("Icon");
            this.nextIcon = this.Q("NextIcon");
            this.tabIndex = -1;
        }

        public string Name { get; private set; }

        public TreeNode<SearchView.Item> Item
        {
            get => this.userData as TreeNode<SearchView.Item>;
            set
            {
                this.userData = value;
                this.icon.style.backgroundImage = value.Value.Icon;
                this.Name = value.Value.Name;
                this.label.text = this.Name;

                this.nextIcon.style.visibility = value.ChildCount == 0 ? Visibility.Hidden : Visibility.Visible;
            }
        }
    }
}
