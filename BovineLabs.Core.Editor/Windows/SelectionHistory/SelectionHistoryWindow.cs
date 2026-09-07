// <copyright file="SelectionHistoryWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

    /// <summary>
    /// Editor window that displays the history of selected objects in Unity.
    /// </summary>
    public sealed class SelectionHistoryWindow : BaseObjectWindow<SelectionHistoryItem, SelectionHistoryService, SelectionHistoryPreferences>
    {
        private readonly List<SelectionHistoryItem> filteredLockedItems = new();
        private readonly List<SelectionHistoryItem> filteredNormalItems = new();

        private double lastLockedClickTime;
        private SelectionHistoryItem lastLockedClickedItem;

        private SelectionHistoryService historyService;
        private ListView lockedItemsListView;
        private Button clearButton;

        /// <inheritdoc/>
        protected override SelectionHistoryService Service => this.historyService ?? SelectionHistoryService.Instance;

        /// <inheritdoc/>
        protected override string StylesheetPath => "Packages/com.bovinelabs.core/BovineLabs.Core.Editor/Windows/SelectionHistory/SelectionHistoryWindow.uss";

        /// <inheritdoc/>
        protected override string RootClassName => "selection-history-window";

        /// <inheritdoc/>
        protected override GUIContent WindowTitle => new("Selection", EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow").image);

        [MenuItem(EditorMenus.RootMenuTools + "Selection Window", priority = 1020)]
        private static void ShowWindow()
        {
            var window = GetWindow<SelectionHistoryWindow>();
            window.titleContent = window.WindowTitle;
            window.Show();
        }

        /// <inheritdoc/>
        protected override void UpdateItemsVisualState()
        {
            base.UpdateItemsVisualState();

            this.lockedItemsListView?.RefreshItems();
        }

        /// <inheritdoc/>
        protected override void InitializeServices()
        {
            this.historyService = SelectionHistoryService.Instance;
            this.historyService.ItemsChanged += this.OnItemsChangedInternal;
        }

        /// <inheritdoc/>
        protected override void CleanupServices()
        {
            if (this.historyService != null)
            {
                this.historyService.ItemsChanged -= this.OnItemsChangedInternal;
            }
        }

        /// <inheritdoc/>
        protected override void SetupAdditionalFeatures(VisualElement root)
        {
            // Create locked items section
            this.CreateLockedItemsSection();

            // Handle window resize to update locked section height
            root.RegisterCallback<GeometryChangedEvent>(this.OnWindowResize);
        }

        /// <inheritdoc/>
        protected override void OnItemsChanged(IReadOnlyList<SelectionHistoryItem> items)
        {
            this.RefreshLockedSection();
        }

        /// <inheritdoc/>
        protected override void CreateCustomToolbarElements(Toolbar toolbar)
        {
            this.clearButton = new ToolbarButton(() => this.historyService?.ClearHistory())
            {
                text = "Clear",
                tooltip = "Clear unlocked selection history",
            };

            toolbar.Add(this.clearButton);
        }

        /// <inheritdoc/>
        protected override Button CreateListItemActionButton()
        {
            var pinButton = new Button();
            pinButton.AddToClassList("history-item-lock-button");
            return pinButton;
        }

        /// <inheritdoc/>
        protected override void BindListItem(VisualElement element, int index)
        {
            if (index >= this.FilteredItems.Count)
            {
                return;
            }

            var item = this.FilteredItems[index];
            this.BindHistoryItem(element, item);
        }

        /// <inheritdoc/>
        protected override void OnListItemClicked(SelectionHistoryItem item)
        {
            this.historyService?.SelectItem(item);
        }

        /// <inheritdoc/>
        protected override void OnListItemDoubleClicked(SelectionHistoryItem item)
        {
            if (item.IsAsset)
            {
                OpenObject(item);
            }
        }

        /// <inheritdoc/>
        protected override void CreateContextMenu(ContextualMenuPopulateEvent evt, SelectionHistoryItem item)
        {
            evt.menu.AppendAction("Select Object", _ => this.historyService?.SelectItem(item));

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
            evt.menu.AppendAction("Remove from History", _ => this.historyService?.RemoveItem(item));
        }

        /// <inheritdoc/>
        protected override string GetStatusText(int totalCount, int filteredCount, int aliveCount)
        {
            var lockedCount = this.filteredLockedItems.Count;
            filteredCount += lockedCount;
            aliveCount += this.filteredLockedItems.Count(item => item.IsAlive);
            return filteredCount == totalCount
                ? $"{totalCount} items ({aliveCount} alive, {lockedCount} locked)"
                : $"{filteredCount} of {totalCount} items ({aliveCount} alive, {lockedCount} locked)";
        }

        /// <inheritdoc/>
        protected override void CreateCustomSettingsMenuItems(DropdownMenu menu)
        {
            var service = this.historyService;
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

        /// <inheritdoc/>
        protected override void CreateSettingsMenu()
        {
            this.CreateStandardSettingsMenu(SelectionHistoryService.PreferenceKey);
        }

        /// <inheritdoc/>
        protected override void PostProcessFilteredItems()
        {
            // Split items into locked and normal for selection history
            this.filteredLockedItems.Clear();
            this.filteredNormalItems.Clear();

            if (this.historyService != null)
            {
                foreach (var item in this.historyService.LockedItems)
                {
                    if (this.ShouldShowItem(item))
                    {
                        this.filteredLockedItems.Add(item);
                    }
                }

                foreach (var item in this.historyService.NormalItems.Reverse())
                {
                    if (this.ShouldShowItem(item))
                    {
                        this.filteredNormalItems.Add(item);
                    }
                }
            }

            // Update main filtered items to show normal items (locked items are in separate list)
            this.FilteredItems.Clear();
            this.FilteredItems.AddRange(this.filteredNormalItems);

            this.lockedItemsListView!.reorderable = !this.IsFiltered();
            this.RefreshLockedSection();
        }

        /// <inheritdoc/>
        protected override void RefreshPreferencesDependentUI()
        {
            base.RefreshPreferencesDependentUI();

            if (this.lockedItemsListView!.fixedItemHeight != this.Service.ItemHeight)
            {
                this.lockedItemsListView.fixedItemHeight = this.Service.ItemHeight;
                this.lockedItemsListView.Rebuild();
            }

            this.UpdateLockedSectionHeight();
        }

        private void CreateLockedItemsSection()
        {
            this.lockedItemsListView = new ListView
            {
                itemsSource = this.filteredLockedItems,
                fixedItemHeight = this.Service.ItemHeight,
                selectionType = SelectionType.None,
                reorderable = true,
                makeItem = this.MakeListItem,
                bindItem = this.BindLockedListItem,
                reorderMode = ListViewReorderMode.Animated,
            };
            this.lockedItemsListView.style.flexShrink = 0;

            this.lockedItemsListView.RegisterCallback<ClickEvent>(this.OnLockedListClick);
            this.lockedItemsListView.AddManipulator(new ContextualMenuManipulator(this.OnLockedListContextMenu));

            this.lockedItemsListView.itemIndexChanged += this.OnLockedItemReordered;

            var index = this.MainListView.parent.IndexOf(this.MainListView);
            this.MainListView.parent.Insert(index, this.lockedItemsListView);
        }

        private void BindLockedListItem(VisualElement element, int index)
        {
            if (index >= this.filteredLockedItems.Count)
            {
                return;
            }

            var item = this.filteredLockedItems[index];
            this.BindHistoryItem(element, item);
        }

        private void BindHistoryItem(VisualElement element, SelectionHistoryItem item)
        {
            this.BindListItemCommon(element, item);
            this.BindPinButton(element, item);
            this.UpdateItemLockClasses(element, item.IsLocked);
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
            this.UpdateLockButtonClasses(pinButton, item.IsLocked);
            this.BindButtonClickAction(pinButton, () => this.historyService?.ToggleLock(item), this.OnPinButtonClick);
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

            var item = this.GetItemAtPosition(this.lockedItemsListView, evt.localPosition);
            if (item != null)
            {
                var currentTime = EditorApplication.timeSinceStartup;
                var timeSinceLastClick = currentTime - this.lastLockedClickTime;

                if (Equals(this.lastLockedClickedItem, item) && timeSinceLastClick < this.Service.DoubleClickThreshold)
                {
                    this.OnListItemDoubleClicked(item);
                    this.lastLockedClickedItem = null;
                }
                else
                {
                    this.OnListItemClicked(item);
                    this.lastLockedClickedItem = item;
                }

                this.lastLockedClickTime = currentTime;
            }
        }

        private void OnLockedListContextMenu(ContextualMenuPopulateEvent evt)
        {
            var item = this.GetItemAtPosition(this.lockedItemsListView, evt.localMousePosition);
            if (item != null)
            {
                this.CreateContextMenu(evt, item);
            }
        }

        private void RefreshLockedSection()
        {
            this.lockedItemsListView?.RefreshItems();
            this.UpdateLockedSectionHeight();
        }

        private void UpdateLockedSectionHeight()
        {
            if (this.lockedItemsListView == null)
            {
                return;
            }

            var itemHeight = this.Service.ItemHeight;
            var lockedItemCount = this.filteredLockedItems.Count;

            if (lockedItemCount == 0)
            {
                // Hide locked section if no items
                this.lockedItemsListView.style.height = 0;
                this.lockedItemsListView.style.display = DisplayStyle.None;
                return;
            }

            // Show locked section
            this.lockedItemsListView.style.display = DisplayStyle.Flex;

            var contentHeight = lockedItemCount * itemHeight;

            // Get total available height (subtract toolbar and status bar)
            var rootHeight = this.MainListView.parent.resolvedStyle.height;
            var toolbarHeight = this.Toolbar.resolvedStyle.height;
            var statusBarHeight = this.Service.ShowStatusBar ? this.StatusBar.resolvedStyle.height : 0f;
            var availableHeight = Mathf.Max(0, rootHeight - toolbarHeight - statusBarHeight);

            if (float.IsNaN(availableHeight))
            {
                return;
            }

            // Apply 80% max height limit, ensuring normal items get at least 20%
            var maxLockedHeight = availableHeight * 0.8f;

            this.lockedItemsListView.style.height = Mathf.Min(contentHeight, maxLockedHeight);
        }

        private void OnPinButtonClick(ClickEvent evt)
        {
            evt.StopPropagation();
        }

        private void OnLockedItemReordered(int fromIndex, int toIndex)
        {
            this.historyService?.ReorderLockedItem(fromIndex, toIndex);
        }

        private bool IsFiltered()
        {
            return !string.IsNullOrEmpty(this.CurrentSearchText) || this.CurrentTypeFilter != "All";
        }

        private void OnWindowResize(GeometryChangedEvent evt)
        {
            this.UpdateLockedSectionHeight();
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
