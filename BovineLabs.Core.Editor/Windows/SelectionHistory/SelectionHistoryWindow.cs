// <copyright file="SelectionHistoryWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.SelectionHistory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Windows.Base;
    using Unity.Serialization.Editor;
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
        private SelectionHistoryItem? lastLockedClickedItem;

        private SelectionHistoryService? historyService;
        private ListView? lockedItemsListView;
        private Button? clearButton;

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
            this.BindListItemCommon(element, item);

            // Handle pin button for history items
            var pinButton = element.Q<Button>();
            if (pinButton != null)
            {
                pinButton.text = string.Empty;
                pinButton.tooltip = item.IsLocked ? "Unlock from top" : "Lock to top";

                if (item.IsLocked)
                {
                    pinButton.RemoveFromClassList("unlocked");
                    pinButton.AddToClassList("locked");
                }
                else
                {
                    pinButton.RemoveFromClassList("locked");
                    pinButton.AddToClassList("unlocked");
                }

                // Clear existing callbacks
                pinButton.UnregisterCallback<ClickEvent>(this.OnPinButtonClick);

                // Remove existing click handler if any
                if (pinButton.userData is Action previousHandler)
                {
                    pinButton.clicked -= previousHandler;
                }

                // Create new click handler
                Action clickHandler = () => this.historyService?.ToggleLock(item);

                pinButton.userData = clickHandler;
                pinButton.clicked += clickHandler;
                pinButton.RegisterCallback<ClickEvent>(this.OnPinButtonClick);

                // Add visual distinction for locked items
                var container = element;
                if (item.IsLocked)
                {
                    container.RemoveFromClassList("unlocked-item");
                    container.AddToClassList("locked-item");
                }
                else
                {
                    container.RemoveFromClassList("locked-item");
                    container.AddToClassList("unlocked-item");
                }
            }
        }

        /// <inheritdoc/>
        protected override void OnListItemClicked(SelectionHistoryItem item)
        {
            this.historyService?.SelectItem(item);
        }

        /// <inheritdoc/>
        protected override void CreateContextMenu(ContextualMenuPopulateEvent evt, SelectionHistoryItem item)
        {
            evt.menu.AppendAction("Select Object", _ => this.historyService?.SelectItem(item));

            if (item.IsAsset)
            {
                evt.menu.AppendAction("Open Object", _ => AssetDatabase.OpenAsset(item.GetObject()));
            }

            if (item.IsAsset)
            {
                evt.menu.AppendAction("Show in Project", _ => EditorGUIUtility.PingObject(item.GetObject()));
            }

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Remove from History", _ => this.historyService?.RemoveItem(item));
        }

        /// <inheritdoc/>
        protected override string GetStatusText(int totalCount, int filteredCount, int aliveCount)
        {
            var lockedCount = this.filteredLockedItems.Count;
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
                var prefs = UserSettings<SelectionHistoryPreferences>.GetOrCreate("Selection History");
                menu.AppendAction("Track Scene Objects", _ =>
                {
                    prefs.TrackSceneObjects = !prefs.TrackSceneObjects;
                    this.RefreshItemsList();
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
                    if (this.ShouldShowItemInternal(item))
                    {
                        this.filteredLockedItems.Add(item);
                    }
                }

                foreach (var item in this.historyService.NormalItems.Reverse())
                {
                    if (this.ShouldShowItemInternal(item))
                    {
                        this.filteredNormalItems.Add(item);
                    }
                }
            }

            // Update main filtered items to show normal items (locked items are in separate list)
            this.FilteredItems.Clear();
            this.FilteredItems.AddRange(this.filteredNormalItems);

            this.RefreshLockedSection();
        }

        /// <inheritdoc/>
        protected override void RefreshPreferencesDependentUI()
        {
            base.RefreshPreferencesDependentUI();

            this.lockedItemsListView!.fixedItemHeight = this.Service.ItemHeight;
            this.lockedItemsListView.Rebuild();
        }

        private void OnLockedListItemDoubleClicked(SelectionHistoryItem item)
        {
            if (item.IsAsset)
            {
                AssetDatabase.OpenAsset(item.GetObject());
            }
        }

        private bool ShouldShowItemInternal(SelectionHistoryItem item)
        {
            if (!string.IsNullOrEmpty(this.CurrentSearchText))
            {
                var searchLower = this.CurrentSearchText.ToLowerInvariant();
                if (!item.Name.ToLowerInvariant().Contains(searchLower) && !item.TypeName.ToLowerInvariant().Contains(searchLower))
                {
                    return false;
                }
            }

            if (this.CurrentTypeFilter != "All" && item.TypeName != this.CurrentTypeFilter)
            {
                return false;
            }

            return true;
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
            this.BindListItemCommon(element, item);

            // Handle pin button for locked items
            var pinButton = element.Q<Button>();
            if (pinButton != null)
            {
                pinButton.text = string.Empty;
                pinButton.tooltip = "Unlock from top";

                pinButton.RemoveFromClassList("unlocked");
                pinButton.AddToClassList("locked");

                // Clear existing callbacks
                pinButton.UnregisterCallback<ClickEvent>(this.OnPinButtonClick);

                if (pinButton.userData is Action previousHandler)
                {
                    pinButton.clicked -= previousHandler;
                }

                Action clickHandler = () => this.historyService?.ToggleLock(item);

                pinButton.userData = clickHandler;
                pinButton.clicked += clickHandler;
                pinButton.RegisterCallback<ClickEvent>(this.OnPinButtonClick);

                var container = element;
                container.RemoveFromClassList("unlocked-item");
                container.AddToClassList("locked-item");
            }
        }

        private void OnLockedListClick(ClickEvent evt)
        {
            var item = this.GetLockedItemAtPosition(evt.localPosition);
            if (item != null)
            {
                var currentTime = EditorApplication.timeSinceStartup;
                var timeSinceLastClick = currentTime - this.lastLockedClickTime;

                if (Equals(this.lastLockedClickedItem, item) && timeSinceLastClick < this.Service.DoubleClickThreshold)
                {
                    this.OnLockedListItemDoubleClicked(item);
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
            var item = this.GetLockedItemAtPosition(evt.localMousePosition);
            if (item != null)
            {
                this.CreateContextMenu(evt, item);
            }
        }

        private SelectionHistoryItem? GetLockedItemAtPosition(Vector2 listLocalPosition)
        {
            var scrollView = this.lockedItemsListView?.Q<ScrollView>();
            if (scrollView == null)
            {
                return null;
            }

            var world = this.lockedItemsListView.LocalToWorld(listLocalPosition);
            if (!scrollView.contentViewport.worldBound.Contains(world))
            {
                return null;
            }

            var contentLocal = scrollView.contentContainer.WorldToLocal(world);
            var itemHeight = this.Service.ItemHeight;
            var index = Mathf.FloorToInt(contentLocal.y / itemHeight);

            if (index >= 0 && index < this.filteredLockedItems.Count)
            {
                return this.filteredLockedItems[index];
            }

            return null;
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
            var rootHeight = this.rootVisualElement.resolvedStyle.height;
            var toolbarHeight = this.Toolbar.resolvedStyle.height;
            var statusBarHeight = this.Service.ShowStatusBar ? 20f : 0f;
            var availableHeight = rootHeight - toolbarHeight - statusBarHeight;

            // Apply 80% max height limit, ensuring normal items get at least 20%
            var maxLockedHeight = availableHeight * 0.8f;

            if (contentHeight <= maxLockedHeight)
            {
                // Content fits naturally - let it size to content (no scrollbar needed)
                this.lockedItemsListView.style.height = contentHeight;
                this.MainListView.style.height = availableHeight - contentHeight;
            }
            else
            {
                // Content exceeds 80% limit - apply fixed height (scrollbar will appear)
                this.lockedItemsListView.style.height = maxLockedHeight;
                this.MainListView.style.height = availableHeight - maxLockedHeight;
            }
        }

        private void OnPinButtonClick(ClickEvent evt)
        {
            evt.StopPropagation();
        }

        private void OnLockedItemReordered(int fromIndex, int toIndex)
        {
            this.historyService?.ReorderLockedItem(fromIndex, toIndex);
        }

        private void OnWindowResize(GeometryChangedEvent evt)
        {
            this.UpdateLockedSectionHeight();
        }
    }
}
