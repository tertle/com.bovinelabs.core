// <copyright file="SearchWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.SearchWindow
{
    using System;
    using System.Collections.Generic;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> Copy of com.unity.platforms\Editor\Unity.Build.Editor\SearchWindow\SearchWindow.cs. </summary>
    public class SearchWindow : EditorWindow
    {
        private SearchView searchView;

        public List<SearchView.Item> Items
        {
            get => this.searchView.Items;
            set => this.searchView.Items = value;
        }

        public string Title
        {
            get => this.searchView.Title;
            set => this.searchView.Title = value;
        }

        private void OnEnable()
        {
            this.searchView = new SearchView();
            this.rootVisualElement.Add(this.searchView);
            this.rootVisualElement.style.color = Color.white;
            this.searchView.OnSelection += e =>
            {
                this.OnSelection?.Invoke(e);
                this.Close();
            };
        }

        private void OnFocus()
        {
            if (this.searchView == null)
            {
                return;
            }

            var searchField = this.searchView.Q<SearchView>();
            var input = searchField.Q("unity-text-input");
            input.Focus();
        }

        private void OnLostFocus()
        {
            this.Close();
        }

        public event Action<SearchView.Item> OnSelection;

        public static SearchWindow Create()
        {
            var window = CreateInstance<SearchWindow>();
            return window;
        }
    }
}
