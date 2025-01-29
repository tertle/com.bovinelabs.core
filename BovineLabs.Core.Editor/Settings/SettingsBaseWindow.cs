// <copyright file="SettingsBaseWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.UI;
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
        private readonly List<TreeViewItemData<IPanelOrGroup>> settingPanelGroups = new();
        private List<TreeViewItemData<IPanelOrGroup>> filteredSettingsPanel = new();

        private VisualElement? contents;
        private Label? contentTitle;
        private IPanelOrGroup? currentSelection;
        private TreeView? tree;
        private ToolbarSearchField? searchField;
        private VisualElement? splitter;
        private VisualElement? toolbar;
        private ToolbarToggle? toggleShowEmpty;

        private float splitterFlex = 0.2f;

        private interface IPanelOrGroup
        {
            public string Name { get; }

            ISettingsPanel? Panel { get; }

            bool MatchesFilter(string searchContext, bool showEmpty);
        }

        /// <summary> Gets the title text for the unity window tab. </summary>
        protected abstract string TitleText { get; }

        protected virtual bool HideToggleShowEmpty => false;

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

            // var flexGrow = this.splitter!.Children().First().resolvedStyle.flexGrow;
            // EditorPrefs.SetFloat(this.SplitterKey, flexGrow);

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

            var id = 0;

            foreach (var (groupName, panelList) in map)
            {
                if (panelList.Count == 1)
                {
                    this.settingPanelGroups.Add(new TreeViewItemData<IPanelOrGroup>(id++, new PanelElement(panelList[0])));
                }
                else
                {
                    var treeGroup = new List<TreeViewItemData<IPanelOrGroup>>(panelList.Count);
                    treeGroup.AddRange(panelList.Select(panel => new TreeViewItemData<IPanelOrGroup>(id++, new PanelElement(panel))));
                    treeGroup.Sort(default(SortAlphabetical));

                    this.settingPanelGroups.Add(new TreeViewItemData<IPanelOrGroup>(id++, new GroupElement(groupName, panelList), treeGroup));
                }
            }

            this.settingPanelGroups.Sort(default(SortAlphabetical));

            // Apply default filters
            this.ApplyFilter(string.Empty, false);
        }

        private void SetupUI()
        {
            // Reference to the root of the window.
            var root = this.rootVisualElement;

            this.settingsWindowTemplate.Clone(root);

            this.searchField = root.Q<ToolbarSearchField>("search");
            this.searchField.Q<TextField>().isDelayed = true;
            this.searchField.RegisterValueChangedCallback(this.SearchFiltering);

            this.splitter = root.Q<VisualElement>("splitter");

            this.toggleShowEmpty = root.Q<ToolbarToggle>("empty");
            if (this.HideToggleShowEmpty)
            {
                this.toggleShowEmpty.value = true;
                this.toggleShowEmpty.RemoveFromHierarchy();
            }

            this.toggleShowEmpty.RegisterValueChangedCallback(this.ShowEmptyToggle);

            this.tree = root.Q<TreeView>("list");
            this.tree.SetRootItems(this.filteredSettingsPanel);
            this.tree.makeItem = () => new Label();
            this.tree.bindItem = (element, index) => ((Label)element).text = this.tree.GetItemDataForIndex<IPanelOrGroup>(index).Name;
            this.tree.selectionChanged += this.SelectionChanged;
            this.tree.fixedItemHeight = 16;

            var contentsView = root.Q<ScrollView>("scroll");
            contentsView.style.flexGrow = 1;
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

            this.tree.selectedIndex = this.filteredSettingsPanel.Count > 0 ? 0 : -1;

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

            this.currentSelection = this.tree!.GetItemDataForIndex<IPanelOrGroup>(this.tree.selectedIndex);
            this.currentSelection?.Panel?.OnActivate(this.searchField!.value, this.contents);
            this.contentTitle!.text = this.currentSelection?.Name ?? string.Empty;
        }

        private void CleanupUI()
        {
            this.searchField.UnregisterValueChangedCallback(this.SearchFiltering);
            this.toggleShowEmpty.UnregisterValueChangedCallback(this.ShowEmptyToggle);
            this.tree!.selectionChanged -= this.SelectionChanged;
            this.toolbar?.Clear();
        }

        private void SearchFiltering(ChangeEvent<string> evt)
        {
            var showEmpty = this.toggleShowEmpty!.value;
            this.FilterChanged(evt.newValue, showEmpty);
        }

        private void ShowEmptyToggle(ChangeEvent<bool> evt)
        {
            var filter = this.searchField!.value;
            this.FilterChanged(filter, evt.newValue);
        }

        private void FilterChanged(string filter, bool showEmpty)
        {
            var selectedID = this.tree!.GetIdForIndex(this.tree!.selectedIndex);

            this.ApplyFilter(filter, showEmpty);

            // this.tree.Rebuild(); // doesn't work, only way I can get it to rebuild is set new reference
            this.tree.SetRootItems(this.filteredSettingsPanel);
            this.tree.RefreshItems();

            this.tree.SetSelectionById(-1); // unselect so we can update filter
            this.tree.SetSelectionById(selectedID);
        }

        private void ApplyFilter(string filter, bool showEmpty)
        {
            // this.filteredSettingsPanel.Clear(); // doesn't work, only way i can get it to rebuild is set new reference
            this.filteredSettingsPanel = new List<TreeViewItemData<IPanelOrGroup>>();

            var filtered = this.settingPanelGroups.FindAll(p => p.data.MatchesFilter(filter, showEmpty));

            for (var f = filtered.Count - 1; f >= 0; f--)
            {
                var data = filtered[f];

                if (data.data is not GroupElement group)
                {
                    continue;
                }

                // We have to filter individual groups
                var treeGroup = new List<TreeViewItemData<IPanelOrGroup>>(group.Panels.Count);
                var filteredGroup = new List<ISettingsPanel>(group.Panels.Count);

                foreach (var c in data.children)
                {
                    if (c.data.MatchesFilter(filter, showEmpty))
                    {
                        treeGroup.Add(c);
                        filteredGroup.Add(c.data.Panel!);
                    }
                }

                filtered[f] = new TreeViewItemData<IPanelOrGroup>(data.id, new GroupElement(data.data.Name, filteredGroup), treeGroup);
            }

            this.filteredSettingsPanel.AddRange(filtered);
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

        private struct SortAlphabetical : IComparer<TreeViewItemData<IPanelOrGroup>>
        {
            public int Compare(TreeViewItemData<IPanelOrGroup> x, TreeViewItemData<IPanelOrGroup> y)
            {
                return string.Compare(x.data.Name, y.data.Name, StringComparison.Ordinal);
            }
        }

        private class PanelElement : IPanelOrGroup
        {
            public PanelElement(ISettingsPanel panel)
            {
                this.Panel = panel;
            }

            public string Name => this.Panel.DisplayName;

            public ISettingsPanel Panel { get; }

            public bool MatchesFilter(string searchContext, bool showEmpty)
            {
                return this.Panel.MatchesFilter(searchContext, showEmpty);
            }
        }

        private class GroupElement : IPanelOrGroup
        {
            public GroupElement(string name, IEnumerable<ISettingsPanel> panelList)
            {
                this.Name = name;
                this.Panels = panelList.ToArray();
            }

            public string Name { get; }

            public ISettingsPanel? Panel => null;

            public IReadOnlyList<ISettingsPanel> Panels { get; }

            public bool MatchesFilter(string searchContext, bool showEmpty)
            {
                return this.Panels.Any(p => p.MatchesFilter(searchContext, showEmpty));
            }
        }
    }
}
