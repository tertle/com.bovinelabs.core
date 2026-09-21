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

    public abstract class SettingsBaseWindow<T> : EditorWindow
        where T : SettingsBaseWindow<T>
    {
        private const string DarkSkinKey = "settings-title-darkmode";
        private const string RootUIPath = "Packages/com.bovinelabs.core/Editor Default Resources/SettingsWindow/";
        private readonly UITemplate _settingsWindowTemplate = new(RootUIPath + "SettingsWindow");

        private readonly List<ISettingsPanel> _settingPanels = new();
        private readonly List<TreeViewItemData<IPanelOrGroup>> _settingPanelGroups = new();
        private List<TreeViewItemData<IPanelOrGroup>> _filteredSettingsPanel = new();

        private VisualElement _contents;
        private Label _contentTitle;
        private IPanelOrGroup _currentSelection;
        private TreeView _tree;
        private ToolbarSearchField _searchField;
        private VisualElement _splitter;
        private VisualElement _toolbar;
        private ToolbarToggle _toggleShowEmpty;

        private float _splitterFlex = 0.2f;

        private interface IPanelOrGroup
        {
            public string Name { get; }

            ISettingsPanel Panel { get; }

            bool MatchesFilter(string searchContext, bool showEmpty);
        }

        protected abstract string TitleText { get; }

        protected virtual bool HideToggleShowEmpty => false;

        private string SplitterKey => $"bl-{TitleText}-splitter";

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
            titleContent.text = TitleText;
            titleContent.image = GetTitleTexture();

            Init();
            SetupUI();

            AfterEnabled();

            EditorApplication.playModeStateChanged += EditorApplicationOnplayModeStateChanged;
        }

        internal void OnDisable()
        {
            BeforeDisabled();

            _settingPanels.Clear();
            _filteredSettingsPanel.Clear();
            _settingPanelGroups.Clear();

            CleanupUI();

            // var flexGrow = this.splitter!.Children().First().resolvedStyle.flexGrow;
            // EditorPrefs.SetFloat(this.SplitterKey, flexGrow);

            EditorApplication.playModeStateChanged -= EditorApplicationOnplayModeStateChanged;
        }

        /// <summary>
        /// Use this hook instead of Unity's OnEnable.
        /// </summary>
        protected virtual void AfterEnabled()
        {
        }

        /// <summary>
        /// Use this hook instead of Unity's OnDisable.
        /// </summary>
        protected virtual void BeforeDisabled()
        {
        }

        protected abstract void GetPanels(List<ISettingsPanel> settingPanels);

        protected virtual Texture GetTitleTexture()
        {
            return EditorGUIUtility.IconContent("Settings").image;
        }

        protected virtual void InitializeToolbar(VisualElement rootElement)
        {
        }

        private static T FindWindowByScope()
        {
            return Resources.FindObjectsOfTypeAll<T>().FirstOrDefault();
        }

        private static T Create()
        {
            return CreateInstance<T>();
        }

        private void Init()
        {
            _splitterFlex = EditorPrefs.GetFloat(SplitterKey, _splitterFlex);

            _settingPanels.Clear();
            _settingPanelGroups.Clear();

            GetPanels(_settingPanels);

            var map = new Dictionary<string, List<ISettingsPanel>>();

            foreach (var p in _settingPanels)
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
                    _settingPanelGroups.Add(new TreeViewItemData<IPanelOrGroup>(id++, new PanelElement(panelList[0])));
                }
                else
                {
                    var treeGroup = new List<TreeViewItemData<IPanelOrGroup>>(panelList.Count);
                    treeGroup.AddRange(panelList.Select(panel => new TreeViewItemData<IPanelOrGroup>(id++, new PanelElement(panel))));
                    treeGroup.Sort(default(SortAlphabetical));

                    _settingPanelGroups.Add(new TreeViewItemData<IPanelOrGroup>(id++, new GroupElement(groupName, panelList), treeGroup));
                }
            }

            _settingPanelGroups.Sort(default(SortAlphabetical));

            // Apply default filters
            ApplyFilter(string.Empty, false);
        }

        private void SetupUI()
        {
            // Reference to the root of the window.
            var root = rootVisualElement;

            _settingsWindowTemplate.Clone(root);

            _searchField = root.Q<ToolbarSearchField>("search");
            _searchField.Q<TextField>().isDelayed = true;
            _searchField.RegisterValueChangedCallback(SearchFiltering);

            _splitter = root.Q<VisualElement>("splitter");

            _toggleShowEmpty = root.Q<ToolbarToggle>("empty");
            if (HideToggleShowEmpty)
            {
                _toggleShowEmpty.value = true;
                _toggleShowEmpty.RemoveFromHierarchy();
            }

            _toggleShowEmpty.RegisterValueChangedCallback(ShowEmptyToggle);

            _tree = root.Q<TreeView>("list");
            _tree.SetRootItems(_filteredSettingsPanel);
            _tree.makeItem = () => new Label();
            _tree.bindItem = (element, index) => ((Label)element).text = _tree.GetItemDataForIndex<IPanelOrGroup>(index).Name;
            _tree.selectionChanged += SelectionChanged;
            _tree.fixedItemHeight = 16;

            var contentsView = root.Q<ScrollView>("scroll");
            contentsView.style.flexGrow = 1;
            _contents = contentsView.Q<VisualElement>("contents");

            _contentTitle = contentsView.Q<Label>("title");

            if (EditorGUIUtility.isProSkin)
            {
                _contentTitle.AddToClassList(DarkSkinKey);
            }
            else
            {
                _contentTitle.RemoveFromClassList(DarkSkinKey);
            }

            _tree.selectedIndex = _filteredSettingsPanel.Count > 0 ? 0 : -1;

            _toolbar = root.Q<VisualElement>("toolbar");
            InitializeToolbar(_toolbar);
        }

        private void SelectionChanged(IEnumerable<object> obj)
        {
            SelectionChanged();
        }

        private void SelectionChanged()
        {
            _currentSelection?.Panel?.OnDeactivate();
            _contents!.Clear();

            _currentSelection = _tree!.GetItemDataForIndex<IPanelOrGroup>(_tree.selectedIndex);
            _currentSelection?.Panel?.OnActivate(_searchField!.value, _contents);
            _contentTitle!.text = _currentSelection?.Name ?? string.Empty;
        }

        private void CleanupUI()
        {
            _searchField.UnregisterValueChangedCallback(SearchFiltering);
            _toggleShowEmpty.UnregisterValueChangedCallback(ShowEmptyToggle);
            _tree!.selectionChanged -= SelectionChanged;
            _toolbar?.Clear();
        }

        private void SearchFiltering(ChangeEvent<string> evt)
        {
            var showEmpty = _toggleShowEmpty!.value;
            FilterChanged(evt.newValue, showEmpty);
        }

        private void ShowEmptyToggle(ChangeEvent<bool> evt)
        {
            var filter = _searchField!.value;
            FilterChanged(filter, evt.newValue);
        }

        private void FilterChanged(string filter, bool showEmpty)
        {
            var selectedID = _tree!.GetIdForIndex(_tree!.selectedIndex);

            ApplyFilter(filter, showEmpty);

            // this.tree.Rebuild(); // doesn't work, only way I can get it to rebuild is set new reference
            _tree.SetRootItems(_filteredSettingsPanel);
            _tree.RefreshItems();

            _tree.SetSelectionById(-1); // unselect so we can update filter
            _tree.SetSelectionById(selectedID);
        }

        private void ApplyFilter(string filter, bool showEmpty)
        {
            // this.filteredSettingsPanel.Clear(); // doesn't work, only way i can get it to rebuild is set new reference
            _filteredSettingsPanel = new List<TreeViewItemData<IPanelOrGroup>>();

            var filtered = _settingPanelGroups.FindAll(p => p.data.MatchesFilter(filter, showEmpty));

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

            _filteredSettingsPanel.AddRange(filtered);
        }

        private void EditorApplicationOnplayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.EnteredEditMode:
                case PlayModeStateChange.EnteredPlayMode:
                    SelectionChanged();
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
                Panel = panel;
            }

            public string Name => Panel.DisplayName;

            public ISettingsPanel Panel { get; }

            public bool MatchesFilter(string searchContext, bool showEmpty)
            {
                return Panel.MatchesFilter(searchContext, showEmpty);
            }
        }

        private class GroupElement : IPanelOrGroup
        {
            public GroupElement(string name, IEnumerable<ISettingsPanel> panelList)
            {
                Name = name;
                Panels = panelList.ToArray();
            }

            public string Name { get; }

            public ISettingsPanel Panel => null;

            public IReadOnlyList<ISettingsPanel> Panels { get; }

            public bool MatchesFilter(string searchContext, bool showEmpty)
            {
                return Panels.Any(p => p.MatchesFilter(searchContext, showEmpty));
            }
        }
    }
}
