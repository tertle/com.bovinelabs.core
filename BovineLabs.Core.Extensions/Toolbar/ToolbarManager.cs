// <copyright file="ToolbarManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_TOOLBAR && (BL_DEBUG || UNITY_EDITOR)
namespace BovineLabs.Core.Toolbar
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.UI;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Mathematics;
    using Unity.Properties;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal interface IToolbarManager : IUIAssetManagement
    {
        void AddGroup<T>(string tabName, string groupName, int assetKey, out int id, out T binding)
            where T : class, IBindingObject, new();

        IBindingObject RemoveGroup(int id);
    }

    /// <summary> Burst data separate to avoid compiling issues with static variables. </summary>
    public static class ToolbarManagerData
    {
        public static readonly SharedStatic<FixedString32Bytes> ActiveTab = SharedStatic<FixedString32Bytes>.GetOrCreate<ActiveTabVar>();

        private struct ActiveTabVar
        {
        }
    }

    /// <summary> The system for the ribbon toolbar. </summary>
    [Configurable]
    internal class ToolbarManager : UIAssetManagement, IToolbarManager
    {
        public const float DefaultUpdateRate = 1 / 4f;

        private const string RootName = "root";
        private const string MenuName = "menu";
        private const string ShowButtonName = "show";
        private const string FilterButtonName = "filter";

        private const string ButtonHighlightClass = "bl-toolbar-highlight";

        [ConfigVar("debug.toolbar", true, "Should the toolbar be hidden", true)]
        private static readonly SharedStatic<bool> Show = SharedStatic<bool>.GetOrCreate<EnabledVar>();

        private readonly Dictionary<string, ToolbarTab> toolbarTabs = new();
        private readonly Dictionary<int, ToolbarTab.Group> toolbarGroups = new();

        private readonly FilterBind filterBind = new();

        [SerializeField]
        private VisualTreeAsset? toolbarAsset;

        private int key;

        private VisualElement panelElement = null!;

        private ToolbarTab? activeTab;

        private VisualElement menuElement = null!;
        private VisualElement rootElement = null!;

        private float uiHeight;
        private bool showRibbon;

        private bool refreshVisible;

        public static IToolbarManager Instance { get; private set; } = new NullToolbarManager();

        private void Awake()
        {
            Instance = this;
            this.menuElement = new VisualElement();

            this.filterBind.ValueChanged += this.FilterBindOnValueChanged;
        }

        private void Start()
        {
            this.LoadAllPanels();

            if (this.toolbarAsset == null)
            {
                throw new Exception("Toolbar not setup");
            }

            this.panelElement = this.toolbarAsset.CloneTree();
            this.panelElement.name = "Toolbar";

            this.OnLoad(this.panelElement);

            if (Show.Data)
            {
                UIDocumentManager.Instance.AddRoot(this.panelElement, -1000);
#if UNITY_EDITOR
                UIDocumentManager.Instance.EditorRebuild += () =>
                {
                    UIDocumentManager.Instance.RemoveRoot(this.panelElement);
                    UIDocumentManager.Instance.AddRoot(this.panelElement, -1000);
                };
#endif
                UIDocumentManager.Instance.Root.RegisterCallback<GeometryChangedEvent>(this.OnRootContentChanged);
            }
        }

        private void Update()
        {
            if (!this.refreshVisible)
            {
                return;
            }

            this.refreshVisible = false;

            var visible = (uint)this.filterBind.Value;
            while (visible != 0)
            {
                var index = (byte)math.tzcnt(visible);
                visible ^= 1U << index;

                if (index >= this.filterBind.Selections.Count)
                {
                    break;
                }

                var groupName = this.filterBind.Selections[index];
                foreach (var t in this.toolbarTabs)
                {
                    foreach (var g in t.Value.Groups)
                    {
                        if (g.Name == groupName)
                        {
                            this.ShowGroup(g);
                        }
                    }
                }
            }
        }

        public void AddGroup<T>(string tabName, string groupName, int assetKey, out int id, out T binding)
            where T : class, IBindingObject, new()
        {
            id = ++this.key;

            var result = this.TryLoadPanel<T>(id, assetKey, out var panel);

            binding = panel.Binding;
            panel.Element.dataSource = panel.Binding;

            if (!this.toolbarTabs.TryGetValue(tabName, out var tab))
            {
                tab = this.toolbarTabs[tabName] = this.CreateTab(tabName);
            }

            var container = new ToolbarGroupContainer(groupName);
            container.Add(panel.Element);

            var group = new ToolbarTab.Group(id, groupName, container, tab);
            this.toolbarGroups.Add(id, group);

            tab.Groups.Add(group);
            tab.Groups.Sort((t1, t2) => string.Compare(t1.Name, t2.Name, StringComparison.Ordinal));

            var filterName = GetFilterName(group);

            this.filterBind.AddSelection(filterName);

            this.refreshVisible = true;
        }

        public IBindingObject RemoveGroup(int id)
        {
            this.TryUnloadPanel(id, out var panel);

            if (!this.toolbarGroups.Remove(id, out var group))
            {
                return panel.Binding;
            }

            var filterName = GetFilterName(group);
            this.filterBind.RemoveSelection(filterName);

            this.HideGroup(group);
            group.Tab.Groups.Remove(group);
            return panel.Binding;
        }

        private static string GetFilterName(ToolbarTab.Group group)
        {
            return group.Name;
        }

        private void ShowGroup(ToolbarTab.Group group)
        {
            // Already visible
            if (group.Container.parent != null)
            {
                 return;
            }

            var insert = FindInsertIndex(group);

            var tab = group.Tab;
            tab.Parent.Insert(insert, group.Container);

            // If this tab is hidden, show it
            if (tab.Button.parent == null)
            {
                this.menuElement.Add(tab.Button);
            }

            // If the first toolbar group loads after the toolbar we want to set it as default
            if (this.activeTab == null)
            {
                this.SetToolbarActive(tab);
            }
        }

        private void HideGroup(ToolbarTab.Group group)
        {
            var tab = group.Tab;

            group.Container.RemoveFromHierarchy();

            // Removed all groups, hide the tab
            if (tab.Parent.childCount == 0)
            {
                tab.Button.RemoveFromHierarchy();

                if (this.activeTab == tab)
                {
                    this.SetDefaultGroup();
                }
            }
        }

        private void OnLoad(VisualElement panel)
        {
            var oldMenuItem = this.menuElement;
            this.menuElement = panel.Q<VisualElement>(MenuName);

            // Move any elements that were added to the temp menu element
            while (oldMenuItem.childCount > 0)
            {
                var child = oldMenuItem[0];
                this.menuElement.Add(child);
            }

            var showButton = panel.Q<Button>(ShowButtonName);
            showButton.clicked += this.ShowToggle;

            var filter = panel.Q<MaskSelectionField>(FilterButtonName);
            filter.dataSource = this.filterBind;

            this.rootElement = panel.Q<VisualElement>(RootName);
            this.rootElement.RegisterCallback<GeometryChangedEvent>(this.OnRootElementGeometryChanged);

            this.uiHeight = Screen.height;

            this.ShowRibbon(this.showRibbon);
            this.SetDefaultGroup();
        }

        private ToolbarTab CreateTab(string tabName)
        {
            var button = new Button { text = tabName };
            var contents = new ToolbarTabElement();
            var toolbarTab = new ToolbarTab(tabName, button, contents);

            button.clicked += () => this.SetToolbarActive(toolbarTab);
            return toolbarTab;
        }

        private void SetDefaultGroup()
        {
            this.SetToolbarActive(null);

            // First is always the toggle button
            if (this.menuElement.childCount == 0)
            {
                return;
            }

            var firstButton = (Button)this.menuElement.contentContainer.Children().First();
            var group = this.toolbarTabs.First(g => g.Value.Button == firstButton);
            this.SetToolbarActive(group.Value);
        }

        private void SetToolbarActive(ToolbarTab? tab)
        {
            if (tab == this.activeTab)
            {
                this.ShowRibbon(true);

                return;
            }

            if (this.activeTab != null)
            {
                this.activeTab.Button.RemoveFromClassList(ButtonHighlightClass);

                // something else has already removed it or moved it
                if (this.activeTab.Parent.parent == this.rootElement)
                {
                    this.rootElement.Remove(this.activeTab.Parent);
                }

                this.activeTab = default;
                ToolbarManagerData.ActiveTab.Data = string.Empty;
            }

            if (tab == null)
            {
                return;
            }

            this.activeTab = tab;
            ToolbarManagerData.ActiveTab.Data = tab.Name;
            tab.Button.AddToClassList(ButtonHighlightClass);

            this.ShowRibbon(true);
        }

        private void ShowRibbon(bool show)
        {
            if (show)
            {
                if (this.activeTab != null)
                {
                    if (this.activeTab.Parent.parent != null)
                    {
                        Assert.IsTrue(this.rootElement == this.activeTab.Parent.parent);
                        return;
                    }

                    this.rootElement.Add(this.activeTab.Parent);
                }
            }
            else
            {
                if (this.activeTab != null)
                {
                    Assert.IsTrue(this.activeTab.Parent.parent == this.rootElement);
                    this.rootElement.Remove(this.activeTab.Parent);
                }
            }

            this.showRibbon = show;
        }

        private void ShowToggle()
        {
            this.ShowRibbon(!this.showRibbon);
        }

        private void OnRootContentChanged(GeometryChangedEvent evt)
        {
            var height = UIDocumentManager.Instance.Root.contentRect.height;

            if (math.abs(this.uiHeight - height) > float.Epsilon)
            {
                this.uiHeight = height;
                this.ResizeViewRect(this.rootElement.contentRect);
            }
        }

        private void OnRootElementGeometryChanged(GeometryChangedEvent evt)
        {
            this.ResizeViewRect(evt.newRect);
        }

        private void ResizeViewRect(Rect uiRect)
        {
            if (this.uiHeight == 0)
            {
                return;
            }

            var cameraHeightNormalized = (this.uiHeight - uiRect.height) / this.uiHeight;

            var cam = Camera.main;
            if (cam != null)
            {
                var rect = cam.rect;
                rect.height = cameraHeightNormalized;
                cam.rect = rect;
            }
        }

        private static int FindInsertIndex(ToolbarTab.Group group)
        {
            var tab = group.Tab;
            var index = tab.Groups.FindIndex(g => g == group);

            // Start index before us
            for (var i = index - 1; i >= 0; i--)
            {
                // tab.Groups is sorted alphabetically so we find the closest active element before this
                if (tab.Groups[i].Container.parent != null)
                {
                    // Then find the index in the visual element
                    return tab.Parent.IndexOf(tab.Groups[i].Container) + 1; // insert after it
                }
            }

            return 0;
        }

        private void FilterBindOnValueChanged((int NewValue, int PreviousValue, IReadOnlyList<string> Selections) v)
        {
            var toRemove = ~v.NewValue & v.PreviousValue;
            var toAdd = v.NewValue & ~v.PreviousValue;

            for (var r = 0; r < v.Selections.Count; r++)
            {
                var mask = (1 << r);

                if ((mask & toRemove) != 0)
                {
                    var s = v.Selections[r];
                    foreach (var g in this.toolbarTabs.SelectMany(t => t.Value.Groups.Where(g => g.Name == s)))
                    {
                        this.HideGroup(g);
                    }
                }
                else if ((mask & toAdd) != 0)
                {
                    var s = v.Selections[r];
                    foreach (var g in this.toolbarTabs.SelectMany(t => t.Value.Groups.Where(g => g.Name == s)))
                    {
                        this.ShowGroup(g);
                    }
                }
            }
        }

        private struct EnabledVar
        {
        }

        private class FilterBind : INotifyBindablePropertyChanged
        {
            private const string SelectionKey = "bl.toolbarmanager.filter.selections";

            private readonly List<string> selectionsValue = new();
            private readonly Dictionary<string, int> selectionsHash = new();
            private HashSet<string> selectionsHidden = new();
            private bool initialized;

            private int value = -1;

            public event EventHandler<BindablePropertyChangedEventArgs>? propertyChanged;

            public event Action<(int NewValue, int PreviousValue, IReadOnlyList<string> Selections)>? ValueChanged;

            [CreateProperty]
            public List<string> Selections => this.selectionsValue;

            [CreateProperty]
            public int Value
            {
                get
                {
                    if (!this.initialized)
                    {
                        this.initialized = true;

                        var selectionSaved = PlayerPrefs.GetString(SelectionKey, string.Empty);
                        var selectionArray = selectionSaved.Split(",");
                        this.selectionsHidden = new HashSet<string>();
                        this.selectionsHidden.UnionWith(selectionArray);
                        this.selectionsHidden.Remove(string.Empty);
                    }

                    return this.value;
                }

                set
                {
                    if (this.value == value)
                    {
                        return;
                    }

                    var removed = (uint)(~value & this.value);
                    var added = (uint)(value & ~this.value);

                    var previousValue = this.value;
                    this.value = value;

                    while (removed != 0)
                    {
                        var index = math.tzcnt(removed);
                        var shifted = (uint)(1 << index);
                        removed ^= shifted;

                        if (index >= this.selectionsValue.Count)
                        {
                            break;
                        }

                        this.selectionsHidden.Add(this.selectionsValue[index]);
                    }

                    while (added != 0)
                    {
                        var index = math.tzcnt(added);
                        var shifted = (uint)(1 << index);
                        added ^= shifted;

                        if (index >= this.selectionsValue.Count)
                        {
                            break;
                        }

                        this.selectionsHidden.Remove(this.selectionsValue[index]);
                    }

                    var serializedString = string.Join(",", this.selectionsHidden);
                    PlayerPrefs.SetString(SelectionKey, serializedString);

                    this.ValueChanged?.Invoke((value, previousValue, this.selectionsValue));
                    this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(nameof(this.Value)));
                }
            }

            public void AddSelection(string filterName)
            {
                this.selectionsHash.TryGetValue(filterName, out var count);
                if (count == 0)
                {
                    this.selectionsValue.Add(filterName);
                    this.selectionsValue.Sort();
                    this.UpdateValue();
                    this.UpdateSelections();
                }

                this.selectionsHash[filterName] = count + 1;
            }

            public void RemoveSelection(string filterName)
            {
                if (!this.selectionsHash.TryGetValue(filterName, out var currentValue))
                {
                    return;
                }

                currentValue--;
                if (currentValue == 0)
                {
                    this.selectionsHash.Remove(filterName);
                    this.selectionsValue.Remove(filterName);
                    this.UpdateSelections();
                }
                else
                {
                    this.selectionsHash[filterName] = currentValue;
                }
            }

            private void UpdateValue()
            {
                var newValue = 0;
                for (var i = 0; i < this.selectionsValue.Count; i++)
                {
                    var selection = this.selectionsValue[i];
                    if (!this.selectionsHidden.Contains(selection))
                    {
                        newValue |= 1 << i;
                    }
                }

                this.Value = newValue;
            }

            private void UpdateSelections()
            {
                this.propertyChanged?.Invoke(this, new BindablePropertyChangedEventArgs(nameof(this.Selections)));
            }
        }

        private class NullToolbarManager : IToolbarManager
        {
            private readonly Dictionary<int, IBindingObject> bindings = new();

            private int key;

            public void AddGroup<T>(string tabName, string groupName, int assetKey, out int id, out T binding)
                where T : class, IBindingObject, new()
            {
                id = key++;
                binding = new T();
                this.bindings.Add(id, binding);
            }

            public IBindingObject RemoveGroup(int id)
            {
                var binding = this.bindings[id];
                this.bindings.Remove(id);
                return binding;
            }

            public object? GetPanel(int id)
            {
                return null;
            }
        }
    }
}
#else
namespace BovineLabs.Core.Toolbar
{
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.UI;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary> The system for the ribbon toolbar. </summary>
    [Configurable]
    internal class ToolbarManager : UIAssetManagement
    {
        [SerializeField]
        private VisualTreeAsset? toolbarAsset;
    }
}
#endif
