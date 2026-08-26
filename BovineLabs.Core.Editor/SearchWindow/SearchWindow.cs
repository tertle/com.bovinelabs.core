// <copyright file="SearchWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#nullable disable
namespace BovineLabs.Core.Editor.SearchWindow
{
    using System;
    using System.Collections.Generic;
    using UnityEditor.Search;
    using UnityEngine;
    using UnityEngine.Search;

    /// <summary> Adapts BovineLabs search items to Unity Search's picker. </summary>
    public sealed class SearchWindow
    {
        private const string ProviderId = "bovinelabs-picker";

        public event Action<SearchView.Item> OnSelection;

        public event Action OnClose;

        public List<SearchView.Item> Items { get; set; } = new();

        public string Title { get; set; } = "Select";

        public Rect Position { get; set; }

        public static SearchWindow Create()
        {
            return new SearchWindow();
        }

        public void Show()
        {
            var title = string.IsNullOrEmpty(this.Title) ? "Select" : this.Title;
            var context = SearchService.CreateContext(this.CreateProvider(title));
            var viewState = new SearchViewState(
                context,
                SearchViewFlags.ListView |
                SearchViewFlags.CompactView |
                SearchViewFlags.DisableInspectorPreview |
                SearchViewFlags.DisableSavedSearchQuery)
            {
                title = title,
                windowTitle = new GUIContent(title),
                position = this.Position,
                excludeClearItem = true,
                selectHandler = this.Select,
            };

            SearchService.ShowPicker(viewState);
        }

        private SearchProvider CreateProvider(string title)
        {
            return new SearchProvider(ProviderId, title)
            {
                fetchItems = this.FetchItems,
            };
        }

        private IEnumerable<SearchItem> FetchItems(SearchContext context, List<SearchItem> searchItems, SearchProvider provider)
        {
            var score = 0;
            for (var i = 0; i < this.Items.Count; i++)
            {
                var item = this.Items[i];
                if (!string.IsNullOrEmpty(context.searchQuery) &&
                    item.Name.IndexOf(context.searchQuery, StringComparison.CurrentCultureIgnoreCase) < 0)
                {
                    continue;
                }

                yield return provider.CreateItem(context, i.ToString(), score++, item.Name, item.Path, item.Icon, item);
            }
        }

        private void Select(SearchItem searchItem, bool canceled)
        {
            if (canceled || searchItem == null)
            {
                this.OnClose?.Invoke();
                return;
            }

            this.OnSelection?.Invoke((SearchView.Item)searchItem.data);
        }
    }
}
