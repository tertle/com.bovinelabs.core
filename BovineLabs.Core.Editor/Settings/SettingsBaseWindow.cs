// <copyright file="SettingsBaseWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using BovineLabs.Core.Editor.UI;
    using BovineLabs.Core.Settings;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> The base settings window that can be used to implement custom drawers. </summary>
    /// <typeparam name="T"> The settings window type. </typeparam>
    public abstract class SettingsBaseWindow<T> : EditorWindow
        where T : SettingsBaseWindow<T>
    {
        private const string DarkSkinKey = "settings-title-darkmode";
        private const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/SettingsWindow/";
        private readonly UITemplate settingsWindowTemplate = new(RootUIPath + "SettingsWindow");

        private readonly List<ISettingsPanel> settingPanels = new();
        private readonly List<SettingsPanelElement> settingPanelGroups = new();
        private readonly List<SettingsPanelElement> filteredSettingsPanel = new();

        private VisualElement? contents;
        private Label? contentTitle;
        private SettingsPanelElement? currentSelection;
        private ListView? list;
        private ToolbarSearchField? searchField;
        private VisualElement? splitter;
        private VisualElement? toolbar;

        private float splitterFlex = 0.2f;

        /// <summary> Gets the title text for the unity window tab. </summary>
        protected abstract string TitleText { get; }

        private string SplitterKey => $"bl-{this.TitleText}-splitter";

        /// <summary> Call this to create and/or open a new settings window. </summary>
        /// <returns> The window instance. </returns>
        public static T Open()
        {
            var window = FindWindowByScope() ?? Create();
            window.Show();
            window.Focus();
            window.minSize = new Vector2(350, 100);
            return window;
        }

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
            this.settingPanelGroups.Clear();

            this.CleanupUI();

            var flexGrow = this.splitter!.Children().First().resolvedStyle.flexGrow;
            EditorPrefs.SetFloat(this.SplitterKey, flexGrow);

            EditorApplication.playModeStateChanged -= this.EditorApplicationOnplayModeStateChanged;
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
        protected virtual Texture GetTitleTexture()
        {
            return EditorGUIUtility.IconContent("Settings").image;
        }

        /// <summary> Implement this to add custom elements to the toolbar. </summary>
        /// <param name="rootElement"> The toolbar root element. </param>
        protected virtual void InitializeToolbar(VisualElement rootElement)
        {
        }

        private static T? FindWindowByScope()
        {
            return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
        }

        private static T Create()
        {
            return CreateInstance<T>();
        }

        private void Init()
        {
            this.splitterFlex = EditorPrefs.GetFloat(this.SplitterKey, this.splitterFlex);

            this.settingPanels.Clear();
            this.settingPanelGroups.Clear();

            this.GetPanels(this.settingPanels);

            var map = new Dictionary<string, List<ISettingsPanel>>();

            foreach (var p in this.settingPanels)
            {
                if (!map.TryGetValue(p.GroupName, out var panelList))
                {
                    panelList = map[p.GroupName] = new List<ISettingsPanel>();
                }

                panelList.Add(p);
            }

            foreach (var (group, panelList) in map)
            {
                if (panelList.Count == 1)
                {
                    this.settingPanelGroups.Add(new SettingsPanelElement(panelList[0].DisplayName, panelList[0]));
                }
                else
                {
                    this.settingPanelGroups.Add(new SettingsPanelElement(group));
                    foreach (var panel in panelList)
                    {
                        this.settingPanelGroups.Add(new SettingsPanelElement($"  {panel.DisplayName}", panel));
                    }
                }
            }

            this.settingPanelGroups.Sort();
            this.filteredSettingsPanel.AddRange(this.settingPanelGroups);
        }

        private void SetupUI()
        {
            // Reference to the root of the window.
            var root = this.rootVisualElement;

            // var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(UxmlPath);
            // uxml.CloneTree(root);
            this.settingsWindowTemplate.Clone(root);

            this.searchField = root.Q<ToolbarSearchField>("search");
            this.searchField.RegisterValueChangedCallback(this.SearchFiltering);

            this.splitter = root.Q<VisualElement>("splitter");

            this.list = root.Q<ListView>("list");
            this.list.itemsSource = this.filteredSettingsPanel;
            this.list.makeItem = () => new Label();
            this.list.bindItem = (element, i) => ((Label)element).text = this.filteredSettingsPanel[i].DisplayName;
            this.list.selectionChanged += this.SelectionChanged;
            this.list.style.flexGrow = this.splitterFlex;
            this.list.fixedItemHeight = 16;

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
            this.currentSelection?.Panel?.OnDeactivate();
            this.contents!.Clear();
            this.contentTitle!.text = string.Empty;

            if ((this.list!.selectedIndex < 0) || (this.list.selectedIndex >= this.filteredSettingsPanel.Count))
            {
                this.currentSelection = null;
                return;
            }

            this.currentSelection = this.filteredSettingsPanel[this.list.selectedIndex];
            this.currentSelection.Panel?.OnActivate(this.searchField!.value, this.contents);
            this.contentTitle.text = this.currentSelection.DisplayName;
        }

        private void CleanupUI()
        {
            this.searchField.UnregisterValueChangedCallback(this.SearchFiltering);
            this.list!.selectionChanged -= this.SelectionChanged;
            this.toolbar?.Clear();
        }

        private void SearchFiltering(ChangeEvent<string> evt)
        {
            this.filteredSettingsPanel.Clear();

            var selected = this.list!.selectedItem;

            if (string.IsNullOrWhiteSpace(evt.newValue))
            {
                this.filteredSettingsPanel.AddRange(this.settingPanelGroups);
            }
            else
            {
                var filtered = this.settingPanelGroups.FindAll(p => p.Panel?.MatchesFilter(evt.newValue) ?? false);
                this.filteredSettingsPanel.AddRange(filtered);
            }

            this.list.Rebuild();

            // Keep selecting the same panel if possible
            var index = this.filteredSettingsPanel.IndexOf((SettingsPanelElement)selected);

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

        private class SettingsPanelElement : IComparable<SettingsPanelElement>
        {
            private readonly string group;

            public SettingsPanelElement(string group)
            {
                this.group = group;
                this.DisplayName = group;
                this.Panel = null;
            }

            public SettingsPanelElement(string displayName, ISettingsPanel panel)
            {
                this.group = panel.GroupName;
                this.DisplayName = displayName;
                this.Panel = panel;
            }

            public string DisplayName { get; }

            public ISettingsPanel? Panel { get; }

            public int CompareTo(SettingsPanelElement? other)
            {
                if (ReferenceEquals(this, other))
                {
                    return 0;
                }

                if (ReferenceEquals(null, other))
                {
                    return 1;
                }

                var groupComparison = string.Compare(this.group, other.group, StringComparison.Ordinal);
                if (groupComparison != 0)
                {
                    return groupComparison;
                }

                if (ReferenceEquals(this.Panel, null))
                {
                    return -1;
                }

                if (ReferenceEquals(null, other.Panel))
                {
                    return 1;
                }

                return string.Compare(this.DisplayName, other.DisplayName, StringComparison.Ordinal);
            }
        }
    }
}
