// <copyright file="SettingsBaseWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> The base settings window that can be used to implement custom drawers. </summary>
    /// <typeparam name="T"> The settings window type. </typeparam>
    public abstract class SettingsBaseWindow<T> : EditorWindow
        where T : SettingsBaseWindow<T>
    {
        private const string UxmlPath = "Packages/com.bovinelabs.basics/BovineLabs.Basics.Editor/Settings/SettingsWindow.uxml";
        private const string DarkSkinKey = "settings-title-darkmode";

        private readonly List<ISettingsPanel> settingPanels = new List<ISettingsPanel>();
        private readonly List<ISettingsPanel> filteredSettingsPanel = new List<ISettingsPanel>();

        private ToolbarSearchField searchField;
        private VisualElement splitter;
        private ListView list;
        private VisualElement contents;
        private Label contentTitle;

        private float splitterFlex = 0.2f;

        private ISettingsPanel currentSelection;
        private VisualElement toolbar;

        /// <summary> Gets the title text for the unity window tab. </summary>
        protected abstract string TitleText { get; }

        private string SplitterKey => $"bl-{this.TitleText}-splitter";

        internal void OnEnable()
        {
            this.titleContent.text = this.TitleText;
            this.titleContent.image = this.GetTitleTexture();

            this.Init();
            this.SetupUI();

            this.AfterEnabled();

            EditorApplication.playModeStateChanged += this.EditorApplicationOnplayModeStateChanged;
        }

        internal void OnDisable()
        {
            this.BeforeDisabled();

            this.settingPanels.Clear();
            this.filteredSettingsPanel.Clear();

            this.CleanupUI();

            var flexGrow = this.splitter.Children().First().resolvedStyle.flexGrow;
            EditorPrefs.SetFloat(this.SplitterKey, flexGrow);

            EditorApplication.playModeStateChanged -= this.EditorApplicationOnplayModeStateChanged;
        }

        /// <summary> Call this to create and/or open a new settings window. </summary>
        /// <returns> The window instance. </returns>
        protected static T Open()
        {
            var window = FindWindowByScope() ?? Create();
            window.Show();
            window.Focus();
            window.minSize = new Vector2(350, 100);
            return window;
        }

        /// <summary> Called after the window is setup. Use this instead of Unity's OnEnabled. </summary>
        protected virtual void AfterEnabled()
        {
        }

        /// <summary> Called before the window is disposed. Use this instead of Unity's OnDisabled. </summary>
        protected virtual void BeforeDisabled()
        {
        }

        /// <summary> Gets the panels for the window. </summary>
        /// <param name="settingPanels"> The panel list to write to. </param>
        protected abstract void GetPanels(List<ISettingsPanel> settingPanels);

        /// <summary> Gets the title texture that'll appear in the Unity window tab. </summary>
        /// <returns> The texture icon. </returns>
        protected virtual Texture GetTitleTexture() => EditorGUIUtility.IconContent("Settings").image;

        /// <summary> Implement this to add custom elements to the toolbar. </summary>
        /// <param name="rootElement"> The toolbar root element. </param>
        protected virtual void InitializeToolbar(VisualElement rootElement)
        {
        }

        private static T FindWindowByScope() => Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();

        private static T Create() => CreateInstance<T>();

        private void Init()
        {
            this.splitterFlex = EditorPrefs.GetFloat(this.SplitterKey, this.splitterFlex);

            this.GetPanels(this.settingPanels);

            this.settingPanels.Sort((p1, p2) => string.Compare(p1.DisplayName, p2.DisplayName, StringComparison.Ordinal));
            this.filteredSettingsPanel.AddRange(this.settingPanels);
        }

        private void SetupUI()
        {
            // Reference to the root of the window.
            var root = this.rootVisualElement;

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            uxml.CloneTree(root);

            this.searchField = root.Q<ToolbarSearchField>("search");
            this.searchField.RegisterValueChangedCallback(this.SearchFiltering);

            this.splitter = root.Q<VisualElement>("splitter");

            this.list = root.Q<ListView>("list");
            this.list.itemsSource = this.filteredSettingsPanel;
            this.list.makeItem = () => new Label();
            this.list.bindItem = (element, i) => ((Label)element).text = this.filteredSettingsPanel[i].DisplayName;
            this.list.onSelectionChange += this.SelectionChanged;
            this.list.style.flexGrow = this.splitterFlex;

            var contentsView = root.Q<ScrollView>("scroll");
            contentsView.style.flexGrow = 1 - this.splitterFlex;
            this.contents = contentsView.Q<VisualElement>("contents");

            this.contentTitle = contentsView.Q<Label>("title");

            if (EditorGUIUtility.isProSkin)
            {
                this.contentTitle.AddToClassList(DarkSkinKey);
            }
            else
            {
                this.contentTitle.RemoveFromClassList(DarkSkinKey);
            }

            this.list.selectedIndex = this.filteredSettingsPanel.Count > 0 ? 0 : -1;

            this.toolbar = root.Q<VisualElement>("toolbar");
            this.InitializeToolbar(this.toolbar);
        }

        private void SelectionChanged(IEnumerable<object> obj)
        {
            this.SelectionChanged();
        }

        private void SelectionChanged()
        {
            this.currentSelection?.OnDeactivate();
            this.contents.Clear();
            this.contentTitle.text = string.Empty;

            if (this.list.selectedIndex < 0 || this.list.selectedIndex >= this.filteredSettingsPanel.Count)
            {
                this.currentSelection = null;
                return;
            }

            this.currentSelection = this.filteredSettingsPanel[this.list.selectedIndex];
            this.currentSelection.OnActivate(this.searchField.value, this.contents);
            this.contentTitle.text = this.currentSelection.DisplayName;
        }

        private void CleanupUI()
        {
            this.searchField.UnregisterValueChangedCallback(this.SearchFiltering);
            this.list.onSelectionChange -= this.SelectionChanged;
            this.toolbar.Clear();
        }

        private void SearchFiltering(ChangeEvent<string> evt)
        {
            this.filteredSettingsPanel.Clear();

            var selected = this.list.selectedItem;

            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                this.filteredSettingsPanel.AddRange(this.settingPanels);
            }
            else
            {
                var filtered = this.settingPanels.FindAll(p => p.MatchesFilter(evt.newValue));
                this.filteredSettingsPanel.AddRange(filtered);
            }

            this.list.Refresh();

            // Keep selecting the same panel if possible
            var index = this.filteredSettingsPanel.IndexOf((ISettingsPanel)selected);

            if (index >= 0)
            {
                this.list.selectedIndex = index;
            }
            else
            {
                this.list.selectedIndex = this.filteredSettingsPanel.Count > 0 ? 0 : -1;
            }

            this.SelectionChanged();
        }

        private void EditorApplicationOnplayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    this.SelectionChanged();
                    break;
            }
        }
    }
}