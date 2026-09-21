namespace BovineLabs.Core.Editor.Windows.Base
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.EditorPreferences;
    using Unity.Entities.Editor.Serialization;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public abstract class BaseObjectWindow<TItem, TService, TPreferences> : EditorWindow, IDisposable
        where TItem : BaseObjectItem
        where TService : BaseObjectService<TItem, TPreferences>
        where TPreferences : BaseDisplayPreferences, new()
    {
        private double _lastClickTime;
        private TItem _lastClickedItem;

        // These flags track services and UI references that do not survive a domain reload.
        [NonSerialized]
        private bool _servicesInitialized;

        [NonSerialized]
        private bool _guiInitialized;

        private UnityEngine.Object _currentSelection;
        private GlobalObjectId _currentSelectionId;

        protected List<TItem> FilteredItems { get; } = new();

        protected ListView MainListView { get; private set; } = null!;

        protected Toolbar Toolbar { get; private set; } = null!;

        protected ToolbarSearchField SearchField { get; private set; }

        protected ToolbarMenu TypeFilterMenu { get; private set; }

        protected ToolbarMenu SettingsMenu { get; private set; }

        protected Label StatusLabel { get; private set; }

        protected VisualElement StatusBar { get; private set; }

        protected string CurrentSearchText { get; private set; } = string.Empty;

        protected string CurrentTypeFilter { get; private set; } = "All";

        protected bool Disposed { get; private set; }

        protected abstract TService Service { get; }

        protected abstract string StylesheetPath { get; }

        protected abstract string RootClassName { get; }

        protected IReadOnlyList<TItem> AllItems => Service.Items;

        protected abstract GUIContent WindowTitle { get; }

        public void CreateGUI()
        {
            if (Disposed)
            {
                return;
            }

            _guiInitialized = false;
            if (!_servicesInitialized)
            {
                InitializeServices();
                Selection.selectionChanged += OnSelectionChanged;
                _servicesInitialized = true;
            }

            titleContent = WindowTitle;
            rootVisualElement.Clear();

            // Recreate the content root so its event callbacks are discarded when Unity rebuilds the window.
            var root = new VisualElement();
            root.style.flexGrow = 1;
            root.AddToClassList(RootClassName);
            rootVisualElement.Add(root);

            LoadStylesheet(root);
            CreateToolbar(root);
            CreateMainContent(root);
            CreateStatusBar(root);
            SetupAdditionalFeatures(root);

            RefreshItemsList();
            _guiInitialized = true;
        }

        public void OnDestroy()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            _guiInitialized = false;
            if (_servicesInitialized)
            {
                CleanupServices();
                Selection.selectionChanged -= OnSelectionChanged;
            }

            Disposed = true;
        }

        protected abstract void InitializeServices();

        protected abstract void CleanupServices();

        protected abstract void SetupAdditionalFeatures(VisualElement root);

        protected abstract void OnItemsChanged(IReadOnlyList<TItem> items);

        protected abstract void CreateCustomToolbarElements(Toolbar toolbar);

        protected virtual VisualElement MakeListItem()
        {
            var container = new VisualElement();
            container.AddToClassList("object-item-container");
            container.style.minHeight = Service.ItemHeight;

            var icon = new Image();
            icon.AddToClassList("object-item-icon");

            var label = new Label();
            label.AddToClassList("object-item-label");

            container.Add(icon);
            container.Add(label);

            var actionButton = CreateListItemActionButton();
            if (actionButton != null)
            {
                container.Add(actionButton);
            }

            return container;
        }

        protected virtual Button CreateListItemActionButton()
        {
            return null;
        }

        protected abstract void BindListItem(VisualElement element, int index);

        protected abstract void OnListItemClicked(TItem item);

        protected virtual void OnListItemDoubleClicked(TItem item)
        {
            // Default implementation does nothing - derived classes can override
        }

        protected abstract void CreateContextMenu(ContextualMenuPopulateEvent evt, TItem item);

        protected abstract string GetStatusText(int totalCount, int filteredCount, int aliveCount);

        protected abstract void CreateCustomSettingsMenuItems(DropdownMenu menu);

        protected virtual VisualElement MakeNoneElement()
        {
            return null;
        }

        protected virtual bool IsReorderable() => false;

        protected virtual void SetupListViewCallbacks()
        {
        }

        protected void RefreshItemsList(bool rebuild = false)
        {
            RefreshSelection();
            FilteredItems.Clear();

            foreach (var item in AllItems)
            {
                item.RefreshMetadata();
                if (ShouldShowItem(item))
                {
                    FilteredItems.Add(item);
                }
            }

            PostProcessFilteredItems();

            if (rebuild)
            {
                MainListView.Rebuild();
            }
            else
            {
                MainListView.RefreshItems();
            }

            UpdateStatusLabel();
        }

        protected virtual void PostProcessFilteredItems()
        {
        }

        protected void UpdateStatusBarVisibility()
        {
            if (StatusBar == null)
            {
                return;
            }

            StatusBar.style.display = Service.ShowStatusBar ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected virtual void RefreshPreferencesDependentUI()
        {
            if (!Mathf.Approximately(MainListView.fixedItemHeight, Service.ItemHeight))
            {
                MainListView.fixedItemHeight = Service.ItemHeight;
                MainListView.Rebuild();
            }

            UpdateStatusBarVisibility();
        }

        protected TItem GetItemAtPosition(Vector2 listLocalPosition)
        {
            return GetItemAtPosition(MainListView, listLocalPosition);
        }

        protected TItem GetItemAtPosition(ListView listView, Vector2 listLocalPosition)
        {
            var scrollView = listView.Q<ScrollView>();
            if (scrollView == null)
            {
                return null;
            }

            var world = listView.LocalToWorld(listLocalPosition);

            // Ignore clicks outside the content viewport (e.g., on scrollbars)
            if (!scrollView.contentViewport.worldBound.Contains(world))
            {
                return null;
            }

            // Resolve the actual bound row, including padding and animated reordering.
            var element = listView.panel.Pick(world);
            while (element != null && element != scrollView.contentContainer)
            {
                if (element.userData is TItem item)
                {
                    return item;
                }

                element = element.parent;
            }

            return null;
        }

        protected virtual void UpdateItemsVisualState()
        {
            RefreshSelection();
            MainListView.RefreshItems();
        }

        protected void OnItemsChangedInternal(IReadOnlyList<TItem> items)
        {
            if (Disposed || !_guiInitialized)
            {
                return;
            }

            RefreshTypeFilterMenu();
            RefreshItemsList();
            RefreshPreferencesDependentUI();
            OnItemsChanged(items);
        }

        protected void BindListItemCommon(VisualElement element, TItem item)
        {
            var itemHeight = Service.ItemHeight;

            // Attach item to element for hit-testing (context menus, etc.)
            element.userData = item;

            element.style.minHeight = itemHeight;

            var icon = element.Q<Image>();
            var label = element.Q<Label>();

            if (icon != null && label != null)
            {
                var scaleFactor = itemHeight / 24f;
                var fontSize = Mathf.RoundToInt(11 * scaleFactor);
                label.style.fontSize = fontSize;

                if (Service.UseMonospaceFont)
                {
                    label.AddToClassList("monospace-font");
                }
                else
                {
                    label.RemoveFromClassList("monospace-font");
                }

                var iconSize = Mathf.RoundToInt(16 * scaleFactor);

                if (Service.ShowIcons && item.Icon != null)
                {
                    icon.image = item.Icon;
                    icon.style.display = DisplayStyle.Flex;
                    icon.style.width = iconSize;
                    icon.style.height = iconSize;
                }
                else
                {
                    icon.style.display = DisplayStyle.None;
                }

                label.text = item.GetDisplayText(Service.ShowTimestamps, Service.ShowAssetPaths, Service.ShowTypeNames,
                    GetTimestampFormat());

                if (!item.IsAlive && Service.GreyOutMissingObjects)
                {
                    label.AddToClassList("missing-object");
                }
                else
                {
                    label.RemoveFromClassList("missing-object");
                }

                ApplySelectionHighlighting(element, item);
            }
        }

        protected void BindButtonClickAction(Button button, Action clickHandler, EventCallback<ClickEvent> clickEventHandler)
        {
            button.UnregisterCallback(clickEventHandler);

            if (button.userData is Action previousHandler)
            {
                button.clicked -= previousHandler;
            }

            button.userData = clickHandler;
            button.clicked += clickHandler;
            button.RegisterCallback(clickEventHandler);
        }

        protected virtual string GetTimestampFormat() => "HH:mm:ss";

        protected void CreateStandardSettingsMenu(string preferencesName)
        {
            if (SettingsMenu == null)
            {
                return;
            }

            SettingsMenu.menu.MenuItems().Clear();

            var preferences = UserSettings<TPreferences>.GetOrCreate(preferencesName);

            // Standard display options
            SettingsMenu.menu.AppendAction("Use Monospace Font", _ =>
            {
                preferences.UseMonospaceFont = !preferences.UseMonospaceFont;
                preferences.OnPreferenceChanged(default);
            }, _ => Service.UseMonospaceFont ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            SettingsMenu.menu.AppendAction("Show Icons", _ =>
            {
                preferences.ShowIcons = !preferences.ShowIcons;
                preferences.OnPreferenceChanged(default);
            }, _ => Service.ShowIcons ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            SettingsMenu.menu.AppendAction("Show Timestamps", _ =>
            {
                preferences.ShowTimestamps = !preferences.ShowTimestamps;
                preferences.OnPreferenceChanged(default);
            }, _ => Service.ShowTimestamps ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            SettingsMenu.menu.AppendAction("Show Asset Paths", _ =>
            {
                preferences.ShowAssetPaths = !preferences.ShowAssetPaths;
                preferences.OnPreferenceChanged(default);
            }, _ => Service.ShowAssetPaths ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            SettingsMenu.menu.AppendAction("Show Type Names", _ =>
            {
                preferences.ShowTypeNames = !preferences.ShowTypeNames;
                preferences.OnPreferenceChanged(default);
            }, _ => Service.ShowTypeNames ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            SettingsMenu.menu.AppendAction("Show Status Bar", _ =>
            {
                preferences.ShowStatusBar = !preferences.ShowStatusBar;
                preferences.OnPreferenceChanged(default);
            }, _ => Service.ShowStatusBar ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            SettingsMenu.menu.AppendSeparator();

            // Advanced display options
            SettingsMenu.menu.AppendAction("Highlight Current Selection", _ =>
            {
                preferences.HighlightCurrentSelection = !preferences.HighlightCurrentSelection;
                preferences.OnPreferenceChanged(default);
            }, _ => Service.HighlightCurrentSelection ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            SettingsMenu.menu.AppendAction("Grey Out Unloaded Objects", _ =>
            {
                preferences.GreyOutUnloadedObjects = !preferences.GreyOutUnloadedObjects;
                preferences.OnPreferenceChanged(default);
            }, _ => Service.GreyOutMissingObjects ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            SettingsMenu.menu.AppendSeparator();

            // Custom menu items specific to each window
            CreateCustomSettingsMenuItems(SettingsMenu.menu);

            SettingsMenu.menu.AppendSeparator();

            // Preferences link
            SettingsMenu.menu.AppendAction("Preferences", _ =>
            {
                SettingsService.OpenUserPreferences("Preferences/" + CoreEditorPreferencesProvider.PreferencesPath);
            });
        }

        protected bool ShouldShowItem(TItem item)
        {
            if (!string.IsNullOrEmpty(CurrentSearchText))
            {
                if (!item.Name.Contains(CurrentSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !item.TypeName.Contains(CurrentSearchText, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            if (CurrentTypeFilter != "All" && item.TypeName != CurrentTypeFilter)
            {
                return false;
            }

            return true;
        }

        protected abstract void CreateSettingsMenu();

        private void LoadStylesheet(VisualElement root)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylesheetPath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
        }

        private void CreateToolbar(VisualElement root)
        {
            Toolbar = new Toolbar();
            Toolbar.AddToClassList("action-toolbar");

            CreateCustomToolbarElements(Toolbar);

            SearchField = new ToolbarSearchField
            {
                value = CurrentSearchText,
                tooltip = "Search by object name or type",
            };

            SearchField.AddToClassList("toolbar-search-field");

            SearchField.RegisterValueChangedCallback(evt =>
            {
                CurrentSearchText = evt.newValue;
                RefreshItemsList();
            });

            Toolbar.Add(SearchField);

            TypeFilterMenu = new ToolbarMenu
            {
                text = CurrentTypeFilter,
                tooltip = "Filter by object type",
            };

            TypeFilterMenu.AddToClassList("toolbar-filter-menu");

            RefreshTypeFilterMenu();
            Toolbar.Add(TypeFilterMenu);

            SettingsMenu = new ToolbarMenu
            {
                tooltip = "Settings",
                variant = ToolbarMenu.Variant.Popup,
            };

            SettingsMenu.AddToClassList("toolbar-settings-menu");

            CreateSettingsMenu();
            Toolbar.Add(SettingsMenu);

            root.Add(Toolbar);
        }

        private void CreateMainContent(VisualElement root)
        {
            MainListView = new ListView
            {
                itemsSource = FilteredItems,
                fixedItemHeight = Service.ItemHeight,
                selectionType = SelectionType.None,
                makeItem = MakeListItem,
                bindItem = BindListItem,
                makeNoneElement = MakeNoneElement,
            };

            if (IsReorderable())
            {
                MainListView.reorderable = true;
                MainListView.reorderMode = ListViewReorderMode.Animated;
            }

            MainListView.AddToClassList("main-listview");

            MainListView.RegisterCallback<ClickEvent>(OnListViewClick);
            MainListView.AddManipulator(new ContextualMenuManipulator(OnContextMenu));

            SetupListViewCallbacks();

            root.Add(MainListView);
        }

        private void CreateStatusBar(VisualElement root)
        {
            StatusBar = new VisualElement();
            StatusBar.AddToClassList($"{RootClassName.Replace("-window", string.Empty)}-status-bar");

            StatusLabel = new Label("Ready");

            StatusBar.Add(StatusLabel);
            root.Add(StatusBar);

            UpdateStatusBarVisibility();
            UpdateStatusLabel();
        }

        private void RefreshTypeFilterMenu()
        {
            if (TypeFilterMenu == null)
            {
                return;
            }

            var allTypes = new List<string> { "All" };
            allTypes.AddRange(AllItems.Select(item => item.TypeName).Distinct().OrderBy(t => t));

            TypeFilterMenu.menu.MenuItems().Clear();

            foreach (var type in allTypes)
            {
                TypeFilterMenu.menu.AppendAction(type, action =>
                {
                    CurrentTypeFilter = action.name;
                    TypeFilterMenu.text = action.name;
                    RefreshItemsList();
                }, action => action.name == CurrentTypeFilter ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            if (!allTypes.Contains(CurrentTypeFilter))
            {
                CurrentTypeFilter = "All";
                TypeFilterMenu.text = "All";
            }
        }

        private void UpdateStatusLabel()
        {
            if (StatusLabel == null)
            {
                return;
            }

            var totalCount = AllItems.Count;
            var filteredCount = FilteredItems.Count;
            var aliveCount = FilteredItems.Count(item => item.IsAlive);

            StatusLabel.text = GetStatusText(totalCount, filteredCount, aliveCount);
        }

        private void OnListViewClick(ClickEvent evt)
        {
            if (evt.button != 0 || evt.target is VisualElement target && (target is Button || target.GetFirstAncestorOfType<Button>() != null))
            {
                return;
            }

            var item = GetItemAtPosition(evt.localPosition);
            if (item != null)
            {
                var currentTime = EditorApplication.timeSinceStartup;
                var timeSinceLastClick = currentTime - _lastClickTime;

                if (Equals(_lastClickedItem, item) && timeSinceLastClick < Service.DoubleClickThreshold)
                {
                    OnListItemDoubleClicked(item);
                    _lastClickedItem = null;
                }
                else
                {
                    OnListItemClicked(item);
                    _lastClickedItem = item;
                }

                _lastClickTime = currentTime;
            }
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            var item = GetItemAtPosition(evt.localMousePosition);
            if (item != null)
            {
                CreateContextMenu(evt, item);
            }
        }

        private void OnSelectionChanged()
        {
            if (Disposed || !_guiInitialized)
            {
                return;
            }

            UpdateItemsVisualState();
        }

        private void OnProjectChange() => RefreshExternalObjectChanges();

        private void OnHierarchyChange() => RefreshExternalObjectChanges();

        private void RefreshExternalObjectChanges()
        {
            if (Disposed || !_guiInitialized)
            {
                return;
            }

            RefreshItemsList();
        }

        private void ApplySelectionHighlighting(VisualElement container, TItem item)
        {
            if (Service.HighlightCurrentSelection)
            {
                if (_currentSelection == null)
                {
                    container.RemoveFromClassList("currently-selected");
                    container.RemoveFromClassList("not-selected");
                    return;
                }

                if (item.MatchesObject(_currentSelection, _currentSelectionId))
                {
                    container.RemoveFromClassList("not-selected");
                    container.AddToClassList("currently-selected");
                }
                else
                {
                    container.RemoveFromClassList("currently-selected");
                    container.AddToClassList("not-selected");
                }
            }
            else
            {
                container.RemoveFromClassList("currently-selected");
                container.RemoveFromClassList("not-selected");
            }
        }

        private void RefreshSelection()
        {
            _currentSelection = Selection.activeObject;
            _currentSelectionId = _currentSelection == null ? default : GlobalObjectId.GetGlobalObjectIdSlow(_currentSelection);
        }
    }
}
