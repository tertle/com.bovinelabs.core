namespace BovineLabs.Core.Editor.Windows.SelectionHistory
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Windows.Base;
    using BovineLabs.Core.Editor.Windows.Favourites;
    using Unity.Entities.Editor.Serialization;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    public sealed class SelectionHistoryWindow : BaseObjectWindow<SelectionHistoryItem, SelectionHistoryService, SelectionHistoryPreferences>
    {
        private readonly List<SelectionHistoryItem> _filteredLockedItems = new();
        private readonly List<SelectionHistoryItem> _filteredNormalItems = new();

        private double _lastLockedClickTime;
        private SelectionHistoryItem _lastLockedClickedItem;

        private SelectionHistoryService _historyService;
        private ListView _lockedItemsListView;
        private Button _clearButton;

        protected override SelectionHistoryService Service => _historyService ?? SelectionHistoryService.Instance;

        protected override string StylesheetPath => "Packages/com.bovinelabs.core/BovineLabs.Core.Editor/Windows/SelectionHistory/SelectionHistoryWindow.uss";

        protected override string RootClassName => "selection-history-window";

        protected override GUIContent WindowTitle => new("Selection", EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow").image);

        [MenuItem(EditorMenus.RootMenuTools + "Selection Window", priority = 1020)]
        private static void ShowWindow()
        {
            var window = GetWindow<SelectionHistoryWindow>();
            window.titleContent = window.WindowTitle;
            window.Show();
        }

        protected override void UpdateItemsVisualState()
        {
            base.UpdateItemsVisualState();

            _lockedItemsListView?.RefreshItems();
        }

        protected override void InitializeServices()
        {
            _historyService = SelectionHistoryService.Instance;
            _historyService.ItemsChanged += OnItemsChangedInternal;
        }

        protected override void CleanupServices()
        {
            if (_historyService != null)
            {
                _historyService.ItemsChanged -= OnItemsChangedInternal;
            }
        }

        protected override void SetupAdditionalFeatures(VisualElement root)
        {
            // Create locked items section
            CreateLockedItemsSection();

            // Handle window resize to update locked section height
            root.RegisterCallback<GeometryChangedEvent>(OnWindowResize);
        }

        protected override void OnItemsChanged(IReadOnlyList<SelectionHistoryItem> items)
        {
            RefreshLockedSection();
        }

        protected override void CreateCustomToolbarElements(Toolbar toolbar)
        {
            _clearButton = new ToolbarButton(() => _historyService?.ClearHistory())
            {
                text = "Clear",
                tooltip = "Clear unlocked selection history",
            };

            toolbar.Add(_clearButton);
        }

        protected override Button CreateListItemActionButton()
        {
            var pinButton = new Button();
            pinButton.AddToClassList("history-item-lock-button");
            return pinButton;
        }

        protected override void BindListItem(VisualElement element, int index)
        {
            if (index >= FilteredItems.Count)
            {
                return;
            }

            var item = FilteredItems[index];
            BindHistoryItem(element, item);
        }

        protected override void OnListItemClicked(SelectionHistoryItem item)
        {
            _historyService?.SelectItem(item);
        }

        protected override void OnListItemDoubleClicked(SelectionHistoryItem item)
        {
            if (item.IsAsset)
            {
                OpenObject(item);
            }
        }

        protected override void CreateContextMenu(ContextualMenuPopulateEvent evt, SelectionHistoryItem item)
        {
            evt.menu.AppendAction("Select Object", _ => _historyService?.SelectItem(item));

            if (item.IsAsset)
            {
                evt.menu.AppendAction("Open Object", _ => OpenObject(item));
                evt.menu.AppendAction("Show in Project", _ =>
                {
                    var obj = item.GetObject();
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                    }
                });

                var favouriteObject = item.GetObject();
                var favourites = FavouritesService.Instance;
                evt.menu.AppendAction("Add to Favourites", _ => favourites.AddFavourite(favouriteObject), _ =>
                {
                    if (!FavouritesService.CanAddFavourite(favouriteObject))
                    {
                        return DropdownMenuAction.Status.Disabled;
                    }

                    return favourites.IsFavourite(favouriteObject)
                        ? DropdownMenuAction.Status.Checked | DropdownMenuAction.Status.Disabled
                        : DropdownMenuAction.Status.Normal;
                });
            }

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Remove from History", _ => _historyService?.RemoveItem(item));
        }

        protected override string GetStatusText(int totalCount, int filteredCount, int aliveCount)
        {
            var lockedCount = _filteredLockedItems.Count;
            filteredCount += lockedCount;
            aliveCount += _filteredLockedItems.Count(item => item.IsAlive);
            return filteredCount == totalCount
                ? $"{totalCount} items ({aliveCount} alive, {lockedCount} locked)"
                : $"{filteredCount} of {totalCount} items ({aliveCount} alive, {lockedCount} locked)";
        }

        protected override void CreateCustomSettingsMenuItems(DropdownMenu menu)
        {
            var service = _historyService;
            if (service != null)
            {
                var prefs = UserSettings<SelectionHistoryPreferences>.GetOrCreate(SelectionHistoryService.PreferenceKey);
                menu.AppendAction("Track Scene Objects", _ =>
                {
                    prefs.TrackSceneObjects = !prefs.TrackSceneObjects;
                    prefs.OnPreferenceChanged(default);
                }, _ => prefs.TrackSceneObjects ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }
        }

        protected override void CreateSettingsMenu()
        {
            CreateStandardSettingsMenu(SelectionHistoryService.PreferenceKey);
        }

        protected override void PostProcessFilteredItems()
        {
            // Split items into locked and normal for selection history
            _filteredLockedItems.Clear();
            _filteredNormalItems.Clear();

            if (_historyService != null)
            {
                foreach (var item in _historyService.LockedItems)
                {
                    if (ShouldShowItem(item))
                    {
                        _filteredLockedItems.Add(item);
                    }
                }

                foreach (var item in _historyService.NormalItems.Reverse())
                {
                    if (ShouldShowItem(item))
                    {
                        _filteredNormalItems.Add(item);
                    }
                }
            }

            // Update main filtered items to show normal items (locked items are in separate list)
            FilteredItems.Clear();
            FilteredItems.AddRange(_filteredNormalItems);

            _lockedItemsListView!.reorderable = !IsFiltered();
            RefreshLockedSection();
        }

        protected override void RefreshPreferencesDependentUI()
        {
            base.RefreshPreferencesDependentUI();

            if (_lockedItemsListView!.fixedItemHeight != Service.ItemHeight)
            {
                _lockedItemsListView.fixedItemHeight = Service.ItemHeight;
                _lockedItemsListView.Rebuild();
            }

            UpdateLockedSectionHeight();
        }

        private void CreateLockedItemsSection()
        {
            _lockedItemsListView = new ListView
            {
                itemsSource = _filteredLockedItems,
                fixedItemHeight = Service.ItemHeight,
                selectionType = SelectionType.None,
                reorderable = true,
                makeItem = MakeListItem,
                bindItem = BindLockedListItem,
                reorderMode = ListViewReorderMode.Animated,
            };
            _lockedItemsListView.style.flexShrink = 0;

            _lockedItemsListView.RegisterCallback<ClickEvent>(OnLockedListClick);
            _lockedItemsListView.AddManipulator(new ContextualMenuManipulator(OnLockedListContextMenu));

            _lockedItemsListView.itemIndexChanged += OnLockedItemReordered;

            var index = MainListView.parent.IndexOf(MainListView);
            MainListView.parent.Insert(index, _lockedItemsListView);
        }

        private void BindLockedListItem(VisualElement element, int index)
        {
            if (index >= _filteredLockedItems.Count)
            {
                return;
            }

            var item = _filteredLockedItems[index];
            BindHistoryItem(element, item);
        }

        private void BindHistoryItem(VisualElement element, SelectionHistoryItem item)
        {
            BindListItemCommon(element, item);
            BindPinButton(element, item);
            UpdateItemLockClasses(element, item.IsLocked);
        }

        private void BindPinButton(VisualElement element, SelectionHistoryItem item)
        {
            var pinButton = element.Q<Button>();
            if (pinButton == null)
            {
                return;
            }

            pinButton.text = string.Empty;
            pinButton.tooltip = item.IsLocked ? "Unlock from top" : "Lock to top";
            UpdateLockButtonClasses(pinButton, item.IsLocked);
            BindButtonClickAction(pinButton, () => _historyService?.ToggleLock(item), OnPinButtonClick);
        }

        private void UpdateLockButtonClasses(VisualElement element, bool isLocked)
        {
            if (isLocked)
            {
                element.RemoveFromClassList("unlocked");
                element.AddToClassList("locked");
            }
            else
            {
                element.RemoveFromClassList("locked");
                element.AddToClassList("unlocked");
            }
        }

        private void UpdateItemLockClasses(VisualElement element, bool isLocked)
        {
            if (isLocked)
            {
                element.RemoveFromClassList("unlocked-item");
                element.AddToClassList("locked-item");
            }
            else
            {
                element.RemoveFromClassList("locked-item");
                element.AddToClassList("unlocked-item");
            }
        }

        private void OnLockedListClick(ClickEvent evt)
        {
            if (evt.button != 0 || evt.target is VisualElement target && (target is Button || target.GetFirstAncestorOfType<Button>() != null))
            {
                return;
            }

            var item = GetItemAtPosition(_lockedItemsListView, evt.localPosition);
            if (item != null)
            {
                var currentTime = EditorApplication.timeSinceStartup;
                var timeSinceLastClick = currentTime - _lastLockedClickTime;

                if (Equals(_lastLockedClickedItem, item) && timeSinceLastClick < Service.DoubleClickThreshold)
                {
                    OnListItemDoubleClicked(item);
                    _lastLockedClickedItem = null;
                }
                else
                {
                    OnListItemClicked(item);
                    _lastLockedClickedItem = item;
                }

                _lastLockedClickTime = currentTime;
            }
        }

        private void OnLockedListContextMenu(ContextualMenuPopulateEvent evt)
        {
            var item = GetItemAtPosition(_lockedItemsListView, evt.localMousePosition);
            if (item != null)
            {
                CreateContextMenu(evt, item);
            }
        }

        private void RefreshLockedSection()
        {
            _lockedItemsListView?.RefreshItems();
            UpdateLockedSectionHeight();
        }

        private void UpdateLockedSectionHeight()
        {
            if (_lockedItemsListView == null)
            {
                return;
            }

            var itemHeight = Service.ItemHeight;
            var lockedItemCount = _filteredLockedItems.Count;

            if (lockedItemCount == 0)
            {
                // Hide locked section if no items
                _lockedItemsListView.style.height = 0;
                _lockedItemsListView.style.display = DisplayStyle.None;
                return;
            }

            // Show locked section
            _lockedItemsListView.style.display = DisplayStyle.Flex;

            var contentHeight = lockedItemCount * itemHeight;

            // Get total available height (subtract toolbar and status bar)
            var rootHeight = MainListView.parent.resolvedStyle.height;
            var toolbarHeight = Toolbar.resolvedStyle.height;
            var statusBarHeight = Service.ShowStatusBar ? StatusBar.resolvedStyle.height : 0f;
            var availableHeight = Mathf.Max(0, rootHeight - toolbarHeight - statusBarHeight);

            if (float.IsNaN(availableHeight))
            {
                return;
            }

            // Apply 80% max height limit, ensuring normal items get at least 20%
            var maxLockedHeight = availableHeight * 0.8f;

            _lockedItemsListView.style.height = Mathf.Min(contentHeight, maxLockedHeight);
        }

        private void OnPinButtonClick(ClickEvent evt)
        {
            evt.StopPropagation();
        }

        private void OnLockedItemReordered(int fromIndex, int toIndex)
        {
            _historyService?.ReorderLockedItem(fromIndex, toIndex);
        }

        private bool IsFiltered()
        {
            return !string.IsNullOrEmpty(CurrentSearchText) || CurrentTypeFilter != "All";
        }

        private void OnWindowResize(GeometryChangedEvent evt)
        {
            UpdateLockedSectionHeight();
        }

        private static void OpenObject(SelectionHistoryItem item)
        {
            var obj = item.GetObject();
            if (obj != null)
            {
                AssetDatabase.OpenAsset(obj);
            }
        }
    }
}
