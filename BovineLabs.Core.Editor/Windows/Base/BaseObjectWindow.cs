// <copyright file="BaseObjectWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

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

    /// <summary>
    /// Base class for editor windows that display lists of Unity objects.
    /// </summary>
    /// <typeparam name="TItem">The type of object item.</typeparam>
    /// <typeparam name="TService">The type of service.</typeparam>
    /// <typeparam name="TPreferences">The type of preferences.</typeparam>
    public abstract class BaseObjectWindow<TItem, TService, TPreferences> : EditorWindow, IDisposable
        where TItem : BaseObjectItem
        where TService : BaseObjectService<TItem, TPreferences>
        where TPreferences : BaseDisplayPreferences, new()
    {
        private double lastClickTime;
        private TItem lastClickedItem;

        // These flags track services and UI references that do not survive a domain reload.
        [NonSerialized]
        private bool servicesInitialized;

        [NonSerialized]
        private bool guiInitialized;

        private UnityEngine.Object currentSelection;
        private GlobalObjectId currentSelectionId;

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

        /// <summary>Gets the service instance for this window.</summary>
        protected abstract TService Service { get; }

        /// <summary>Gets the stylesheet path for this window.</summary>
        protected abstract string StylesheetPath { get; }

        /// <summary>Gets the CSS class name for the root element.</summary>
        protected abstract string RootClassName { get; }

        /// <summary>Gets all items to display in the list.</summary>
        protected IReadOnlyList<TItem> AllItems => this.Service.Items;

        /// <summary>Gets the window title content.</summary>
        protected abstract GUIContent WindowTitle { get; }

        public void CreateGUI()
        {
            if (this.Disposed)
            {
                return;
            }

            this.guiInitialized = false;
            if (!this.servicesInitialized)
            {
                this.InitializeServices();
                Selection.selectionChanged += this.OnSelectionChanged;
                this.servicesInitialized = true;
            }

            this.titleContent = this.WindowTitle;
            this.rootVisualElement.Clear();

            // Recreate the content root so its event callbacks are discarded when Unity rebuilds the window.
            var root = new VisualElement();
            root.style.flexGrow = 1;
            root.AddToClassList(this.RootClassName);
            this.rootVisualElement.Add(root);

            this.LoadStylesheet(root);
            this.CreateToolbar(root);
            this.CreateMainContent(root);
            this.CreateStatusBar(root);
            this.SetupAdditionalFeatures(root);

            this.RefreshItemsList();
            this.guiInitialized = true;
        }

        public void OnDestroy()
        {
            this.Dispose();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (this.Disposed)
            {
                return;
            }

            this.guiInitialized = false;
            if (this.servicesInitialized)
            {
                this.CleanupServices();
                Selection.selectionChanged -= this.OnSelectionChanged;
            }

            this.Disposed = true;
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
            container.style.minHeight = this.Service.ItemHeight;

            var icon = new Image();
            icon.AddToClassList("object-item-icon");

            var label = new Label();
            label.AddToClassList("object-item-label");

            container.Add(icon);
            container.Add(label);

            var actionButton = this.CreateListItemActionButton();
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
            this.RefreshSelection();
            this.FilteredItems.Clear();

            foreach (var item in this.AllItems)
            {
                item.RefreshMetadata();
                if (this.ShouldShowItem(item))
                {
                    this.FilteredItems.Add(item);
                }
            }

            this.PostProcessFilteredItems();

            if (rebuild)
            {
                this.MainListView.Rebuild();
            }
            else
            {
                this.MainListView.RefreshItems();
            }

            this.UpdateStatusLabel();
        }

        protected virtual void PostProcessFilteredItems()
        {
        }

        protected void UpdateStatusBarVisibility()
        {
            if (this.StatusBar == null)
            {
                return;
            }

            this.StatusBar.style.display = this.Service.ShowStatusBar ? DisplayStyle.Flex : DisplayStyle.None;
        }

        protected virtual void RefreshPreferencesDependentUI()
        {
            if (!Mathf.Approximately(this.MainListView.fixedItemHeight, this.Service.ItemHeight))
            {
                this.MainListView.fixedItemHeight = this.Service.ItemHeight;
                this.MainListView.Rebuild();
            }

            this.UpdateStatusBarVisibility();
        }

        protected TItem GetItemAtPosition(Vector2 listLocalPosition)
        {
            return this.GetItemAtPosition(this.MainListView, listLocalPosition);
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
            this.RefreshSelection();
            this.MainListView.RefreshItems();
        }

        protected void OnItemsChangedInternal(IReadOnlyList<TItem> items)
        {
            if (this.Disposed || !this.guiInitialized)
            {
                return;
            }

            this.RefreshTypeFilterMenu();
            this.RefreshItemsList();
            this.RefreshPreferencesDependentUI();
            this.OnItemsChanged(items);
        }

        protected void BindListItemCommon(VisualElement element, TItem item)
        {
            var itemHeight = this.Service.ItemHeight;

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

                if (this.Service.UseMonospaceFont)
                {
                    label.AddToClassList("monospace-font");
                }
                else
                {
                    label.RemoveFromClassList("monospace-font");
                }

                var iconSize = Mathf.RoundToInt(16 * scaleFactor);

                if (this.Service.ShowIcons && item.Icon != null)
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

                label.text = item.GetDisplayText(this.Service.ShowTimestamps, this.Service.ShowAssetPaths, this.Service.ShowTypeNames,
                    this.GetTimestampFormat());

                if (!item.IsAlive && this.Service.GreyOutMissingObjects)
                {
                    label.AddToClassList("missing-object");
                }
                else
                {
                    label.RemoveFromClassList("missing-object");
                }

                this.ApplySelectionHighlighting(element, item);
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

        /// <summary>
        /// Creates the standard settings menu with common display options.
        /// </summary>
        /// <param name="preferencesName">The name used for UserSettings.</param>
        protected void CreateStandardSettingsMenu(string preferencesName)
        {
            if (this.SettingsMenu == null)
            {
                return;
            }

            this.SettingsMenu.menu.MenuItems().Clear();

            var preferences = UserSettings<TPreferences>.GetOrCreate(preferencesName);

            // Standard display options
            this.SettingsMenu.menu.AppendAction("Use Monospace Font", _ =>
            {
                preferences.UseMonospaceFont = !preferences.UseMonospaceFont;
                preferences.OnPreferenceChanged(default);
            }, _ => this.Service.UseMonospaceFont ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            this.SettingsMenu.menu.AppendAction("Show Icons", _ =>
            {
                preferences.ShowIcons = !preferences.ShowIcons;
                preferences.OnPreferenceChanged(default);
            }, _ => this.Service.ShowIcons ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            this.SettingsMenu.menu.AppendAction("Show Timestamps", _ =>
            {
                preferences.ShowTimestamps = !preferences.ShowTimestamps;
                preferences.OnPreferenceChanged(default);
            }, _ => this.Service.ShowTimestamps ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            this.SettingsMenu.menu.AppendAction("Show Asset Paths", _ =>
            {
                preferences.ShowAssetPaths = !preferences.ShowAssetPaths;
                preferences.OnPreferenceChanged(default);
            }, _ => this.Service.ShowAssetPaths ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            this.SettingsMenu.menu.AppendAction("Show Type Names", _ =>
            {
                preferences.ShowTypeNames = !preferences.ShowTypeNames;
                preferences.OnPreferenceChanged(default);
            }, _ => this.Service.ShowTypeNames ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            this.SettingsMenu.menu.AppendAction("Show Status Bar", _ =>
            {
                preferences.ShowStatusBar = !preferences.ShowStatusBar;
                preferences.OnPreferenceChanged(default);
            }, _ => this.Service.ShowStatusBar ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            this.SettingsMenu.menu.AppendSeparator();

            // Advanced display options
            this.SettingsMenu.menu.AppendAction("Highlight Current Selection", _ =>
            {
                preferences.HighlightCurrentSelection = !preferences.HighlightCurrentSelection;
                preferences.OnPreferenceChanged(default);
            }, _ => this.Service.HighlightCurrentSelection ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            this.SettingsMenu.menu.AppendAction("Grey Out Unloaded Objects", _ =>
            {
                preferences.GreyOutUnloadedObjects = !preferences.GreyOutUnloadedObjects;
                preferences.OnPreferenceChanged(default);
            }, _ => this.Service.GreyOutMissingObjects ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);

            this.SettingsMenu.menu.AppendSeparator();

            // Custom menu items specific to each window
            this.CreateCustomSettingsMenuItems(this.SettingsMenu.menu);

            this.SettingsMenu.menu.AppendSeparator();

            // Preferences link
            this.SettingsMenu.menu.AppendAction("Preferences", _ =>
            {
                SettingsService.OpenUserPreferences("Preferences/" + CoreEditorPreferencesProvider.PreferencesPath);
            });
        }

        protected bool ShouldShowItem(TItem item)
        {
            if (!string.IsNullOrEmpty(this.CurrentSearchText))
            {
                if (!item.Name.Contains(this.CurrentSearchText, StringComparison.OrdinalIgnoreCase) &&
                    !item.TypeName.Contains(this.CurrentSearchText, StringComparison.OrdinalIgnoreCase))
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

        protected abstract void CreateSettingsMenu();

        private void LoadStylesheet(VisualElement root)
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(this.StylesheetPath);
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }
        }

        private void CreateToolbar(VisualElement root)
        {
            this.Toolbar = new Toolbar();
            this.Toolbar.AddToClassList("action-toolbar");

            this.CreateCustomToolbarElements(this.Toolbar);

            this.SearchField = new ToolbarSearchField
            {
                value = this.CurrentSearchText,
                tooltip = "Search by object name or type",
            };

            this.SearchField.AddToClassList("toolbar-search-field");

            this.SearchField.RegisterValueChangedCallback(evt =>
            {
                this.CurrentSearchText = evt.newValue;
                this.RefreshItemsList();
            });

            this.Toolbar.Add(this.SearchField);

            this.TypeFilterMenu = new ToolbarMenu
            {
                text = this.CurrentTypeFilter,
                tooltip = "Filter by object type",
            };

            this.TypeFilterMenu.AddToClassList("toolbar-filter-menu");

            this.RefreshTypeFilterMenu();
            this.Toolbar.Add(this.TypeFilterMenu);

            this.SettingsMenu = new ToolbarMenu
            {
                tooltip = "Settings",
                variant = ToolbarMenu.Variant.Popup,
            };

            this.SettingsMenu.AddToClassList("toolbar-settings-menu");

            this.CreateSettingsMenu();
            this.Toolbar.Add(this.SettingsMenu);

            root.Add(this.Toolbar);
        }

        private void CreateMainContent(VisualElement root)
        {
            this.MainListView = new ListView
            {
                itemsSource = this.FilteredItems,
                fixedItemHeight = this.Service.ItemHeight,
                selectionType = SelectionType.None,
                makeItem = this.MakeListItem,
                bindItem = this.BindListItem,
                makeNoneElement = this.MakeNoneElement,
            };

            if (this.IsReorderable())
            {
                this.MainListView.reorderable = true;
                this.MainListView.reorderMode = ListViewReorderMode.Animated;
            }

            this.MainListView.AddToClassList("main-listview");

            this.MainListView.RegisterCallback<ClickEvent>(this.OnListViewClick);
            this.MainListView.AddManipulator(new ContextualMenuManipulator(this.OnContextMenu));

            this.SetupListViewCallbacks();

            root.Add(this.MainListView);
        }

        private void CreateStatusBar(VisualElement root)
        {
            this.StatusBar = new VisualElement();
            this.StatusBar.AddToClassList($"{this.RootClassName.Replace("-window", string.Empty)}-status-bar");

            this.StatusLabel = new Label("Ready");

            this.StatusBar.Add(this.StatusLabel);
            root.Add(this.StatusBar);

            this.UpdateStatusBarVisibility();
            this.UpdateStatusLabel();
        }

        private void RefreshTypeFilterMenu()
        {
            if (this.TypeFilterMenu == null)
            {
                return;
            }

            var allTypes = new List<string> { "All" };
            allTypes.AddRange(this.AllItems.Select(item => item.TypeName).Distinct().OrderBy(t => t));

            this.TypeFilterMenu.menu.MenuItems().Clear();

            foreach (var type in allTypes)
            {
                this.TypeFilterMenu.menu.AppendAction(type, action =>
                {
                    this.CurrentTypeFilter = action.name;
                    this.TypeFilterMenu.text = action.name;
                    this.RefreshItemsList();
                }, action => action.name == this.CurrentTypeFilter ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal);
            }

            if (!allTypes.Contains(this.CurrentTypeFilter))
            {
                this.CurrentTypeFilter = "All";
                this.TypeFilterMenu.text = "All";
            }
        }

        private void UpdateStatusLabel()
        {
            if (this.StatusLabel == null)
            {
                return;
            }

            var totalCount = this.AllItems.Count;
            var filteredCount = this.FilteredItems.Count;
            var aliveCount = this.FilteredItems.Count(item => item.IsAlive);

            this.StatusLabel.text = this.GetStatusText(totalCount, filteredCount, aliveCount);
        }

        private void OnListViewClick(ClickEvent evt)
        {
            if (evt.button != 0 || evt.target is VisualElement target && (target is Button || target.GetFirstAncestorOfType<Button>() != null))
            {
                return;
            }

            var item = this.GetItemAtPosition(evt.localPosition);
            if (item != null)
            {
                var currentTime = EditorApplication.timeSinceStartup;
                var timeSinceLastClick = currentTime - this.lastClickTime;

                if (Equals(this.lastClickedItem, item) && timeSinceLastClick < this.Service.DoubleClickThreshold)
                {
                    this.OnListItemDoubleClicked(item);
                    this.lastClickedItem = null;
                }
                else
                {
                    this.OnListItemClicked(item);
                    this.lastClickedItem = item;
                }

                this.lastClickTime = currentTime;
            }
        }

        private void OnContextMenu(ContextualMenuPopulateEvent evt)
        {
            var item = this.GetItemAtPosition(evt.localMousePosition);
            if (item != null)
            {
                this.CreateContextMenu(evt, item);
            }
        }

        private void OnSelectionChanged()
        {
            if (this.Disposed || !this.guiInitialized)
            {
                return;
            }

            this.UpdateItemsVisualState();
        }

        private void OnProjectChange() => this.RefreshExternalObjectChanges();

        private void OnHierarchyChange() => this.RefreshExternalObjectChanges();

        private void RefreshExternalObjectChanges()
        {
            if (this.Disposed || !this.guiInitialized)
            {
                return;
            }

            this.RefreshItemsList();
        }

        private void ApplySelectionHighlighting(VisualElement container, TItem item)
        {
            if (this.Service.HighlightCurrentSelection)
            {
                if (this.currentSelection == null)
                {
                    container.RemoveFromClassList("currently-selected");
                    container.RemoveFromClassList("not-selected");
                    return;
                }

                if (item.MatchesObject(this.currentSelection, this.currentSelectionId))
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
            this.currentSelection = Selection.activeObject;
            this.currentSelectionId = this.currentSelection == null ? default : GlobalObjectId.GetGlobalObjectIdSlow(this.currentSelection);
        }
    }
}
