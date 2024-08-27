// <copyright file="SearchWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#nullable disable
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
        public const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/SearchWindow/";

        private SearchView searchView;

        public event Action<SearchView.Item> OnSelection;

        public event Action OnClose;

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

        public static SearchWindow Create()
        {
            var window = CreateInstance<SearchWindow>();
            return window;
        }

        private void OnEnable()
        {
            this.searchView = new SearchView();
            this.rootVisualElement.Add(this.searchView);
            this.rootVisualElement.style.color = Color.white;
            this.searchView.OnSelection += e =>
            {
                this.OnSelection?.Invoke(e);
                this.Close(false);
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
            this.Close(true);
        }

        private void Close(bool fireEvent)
        {
            this.Close();

            if (fireEvent)
            {
                this.OnClose?.Invoke();
            }
        }
    }
}
