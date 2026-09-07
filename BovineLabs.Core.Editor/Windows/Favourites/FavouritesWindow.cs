// <copyright file="FavouritesWindow.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.Favourites
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Windows.Base;
    using Unity.Entities.Editor.Serialization;
    using UnityEditor;
    using UnityEditor.UIElements;
    using UnityEngine;
    using UnityEngine.UIElements;

    /// <summary>
    /// Editor window that displays and manages a collection of favourite objects in Unity.
    /// </summary>
    public sealed class FavouritesWindow : BaseObjectWindow<FavouritesItem, FavouritesService, FavouritesPreferences>
    {
        private FavouritesService favouritesService;
        private VisualElement dropLabel;
        private Button addSelectionButton;

        /// <inheritdoc/>
        protected override FavouritesService Service => this.favouritesService ?? FavouritesService.Instance;

        /// <inheritdoc/>
        protected override string StylesheetPath => "Packages/com.bovinelabs.core/BovineLabs.Core.Editor/Windows/Favourites/FavouritesWindow.uss";

        /// <inheritdoc/>
        protected override string RootClassName => "favourites-window";

        /// <inheritdoc/>
        protected override GUIContent WindowTitle => new("Favourites", EditorGUIUtility.IconContent("Favorite Icon").image);

        [MenuItem(EditorMenus.RootMenuTools + "Favourites Window", priority = 1021)]
        private static void ShowWindow()
        {
            var window = GetWindow<FavouritesWindow>();
            window.titleContent = window.WindowTitle;
            window.Show();
        }

        /// <inheritdoc/>
        protected override void InitializeServices()
        {
            this.favouritesService = FavouritesService.Instance;
            this.favouritesService.ItemsChanged += this.OnItemsChangedInternal;
        }

        /// <inheritdoc/>
        protected override void CleanupServices()
        {
            if (this.favouritesService != null)
            {
                this.favouritesService.ItemsChanged -= this.OnItemsChangedInternal;
            }
        }

        /// <inheritdoc/>
        protected override void SetupAdditionalFeatures(VisualElement root)
        {
            this.SetupDragAndDrop(root);
        }

        /// <inheritdoc/>
        protected override void OnItemsChanged(IReadOnlyList<FavouritesItem> items)
        {
        }

        /// <inheritdoc/>
        protected override void UpdateItemsVisualState()
        {
            base.UpdateItemsVisualState();
            this.UpdateAddSelectionButton();
        }

        /// <inheritdoc/>
        protected override void CreateCustomToolbarElements(Toolbar toolbar)
        {
            this.addSelectionButton = new ToolbarButton(() => this.favouritesService.AddFavourites(Selection.objects))
            {
                text = "Add Selection",
                tooltip = "Add selected assets to favourites",
            };

            toolbar.Add(this.addSelectionButton);
            this.UpdateAddSelectionButton();
        }

        /// <inheritdoc/>
        protected override Button CreateListItemActionButton()
        {
            var openButton = new Button();
            openButton.AddToClassList("favourite-item-open-button");
            openButton.tooltip = "Open object";
            return openButton;
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

            // Handle open button
            var openButton = element.Q<Button>();
            if (openButton != null)
            {
                openButton.SetEnabled(item.IsAsset);
                this.BindButtonClickAction(openButton, () => OpenItem(item), this.OnOpenButtonClick);
            }
        }

        /// <inheritdoc/>
        protected override void OnListItemClicked(FavouritesItem item)
        {
            this.favouritesService?.SelectFromFavourites(item);
        }

        /// <inheritdoc/>
        protected override void OnListItemDoubleClicked(FavouritesItem item)
        {
            OpenItem(item);
        }

        /// <inheritdoc/>
        protected override void CreateContextMenu(ContextualMenuPopulateEvent evt, FavouritesItem item)
        {
            evt.menu.AppendAction("Select Object", _ => this.favouritesService?.SelectFromFavourites(item));

            if (item.IsAsset)
            {
                evt.menu.AppendAction("Open Object", _ => OpenItem(item));
                evt.menu.AppendAction("Show in Project", _ =>
                {
                    var obj = item.GetObject();
                    if (obj != null)
                    {
                        EditorGUIUtility.PingObject(obj);
                    }
                });
            }

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Remove from Favourites",
                _ =>
                {
                    if (this.favouritesService?.ConfirmRemoval == true)
                    {
                        if (!EditorUtility.DisplayDialog("Remove Favourite", $"Remove '{item.Name}' from favourites?", "Remove", "Cancel"))
                        {
                            return;
                        }
                    }

                    this.favouritesService?.RemoveItem(item);
                });
        }

        /// <inheritdoc/>
        protected override string GetStatusText(int totalCount, int filteredCount, int aliveCount)
        {
            return filteredCount == totalCount
                ? $"{totalCount} favourites ({aliveCount} alive)"
                : $"{filteredCount} of {totalCount} favourites ({aliveCount} alive)";
        }

        /// <inheritdoc/>
        protected override void CreateCustomSettingsMenuItems(DropdownMenu menu)
        {
            var prefs = UserSettings<FavouritesPreferences>.GetOrCreate(FavouritesService.PreferenceKey);
            menu.AppendAction("Confirm Removal", _ =>
            {
                prefs.ConfirmRemoval = !prefs.ConfirmRemoval;
                prefs.OnPreferenceChanged(default);
            }, _ => prefs.ConfirmRemoval
                ? DropdownMenuAction.Status.Checked
                : DropdownMenuAction.Status.Normal);

            menu.AppendSeparator();

            menu.AppendAction("Clear All Favourites", _ =>
            {
                if (!EditorUtility.DisplayDialog("Clear All Favourites", "Are you sure you want to remove all favourites? This action cannot be undone.",
                    "Clear All", "Cancel"))
                {
                    return;
                }

                this.favouritesService?.ClearFavourites();
            });
        }

        /// <inheritdoc/>
        protected override void CreateSettingsMenu()
        {
            this.CreateStandardSettingsMenu(FavouritesService.PreferenceKey);
        }

        /// <inheritdoc/>
        protected override bool IsReorderable() => !this.IsFiltered();

        /// <inheritdoc/>
        protected override void SetupListViewCallbacks()
        {
            this.MainListView.reorderMode = ListViewReorderMode.Animated;
            this.MainListView.itemIndexChanged += this.OnFavouriteItemReordered;
        }

        /// <inheritdoc/>
        protected override void PostProcessFilteredItems()
        {
            // Update reorderable state
            this.MainListView!.reorderable = !this.IsFiltered();
            this.UpdateAddSelectionButton();
        }

        /// <inheritdoc/>
        protected override string GetTimestampFormat() => "yyyy-MM-dd HH:mm";

        /// <inheritdoc/>
        protected override VisualElement MakeNoneElement()
        {
            var container = new VisualElement();
            container.AddToClassList("favourites-empty-container");

            var label = new Label("Drag assets here to add to favourites");
            label.AddToClassList("favourites-empty-label");

            container.Add(label);
            return container;
        }

        private bool IsFiltered()
        {
            return !string.IsNullOrEmpty(this.CurrentSearchText) || this.CurrentTypeFilter != "All";
        }

        private void UpdateAddSelectionButton()
        {
            this.addSelectionButton.SetEnabled(Selection.objects.Any(obj =>
                FavouritesService.CanAddFavourite(obj) && !this.favouritesService.IsFavourite(obj)));
        }

        private void SetupDragAndDrop(VisualElement root)
        {
            this.dropLabel = new VisualElement { pickingMode = PickingMode.Ignore };
            this.dropLabel.AddToClassList("favourites-drop-overlay");
            this.dropLabel.RegisterCallback<AttachToPanelEvent>(evt =>
                evt.destinationPanel.visualTree.RegisterCallback<DragExitedEvent>(this.OnDragExited));
            this.dropLabel.RegisterCallback<DetachFromPanelEvent>(evt =>
                evt.originPanel.visualTree.UnregisterCallback<DragExitedEvent>(this.OnDragExited));

            root.Add(this.dropLabel);

            root.RegisterCallback<DragEnterEvent>(_ =>
            {
                this.UpdateDropOverlay();
            });

            root.RegisterCallback<DragLeaveEvent>(_ =>
            {
                this.dropLabel.style.display = DisplayStyle.None;
            });

            root.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.objectReferences.Length == 0)
                {
                    return;
                }

                var hasValidAssets = this.UpdateDropOverlay();
                DragAndDrop.visualMode = hasValidAssets ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
            });

            root.RegisterCallback<DragPerformEvent>(evt =>
            {
                this.dropLabel.style.display = DisplayStyle.None;

                var assetObjects = DragAndDrop.objectReferences.Where(FavouritesService.CanAddFavourite).ToArray();
                if (assetObjects.Length == 0)
                {
                    return;
                }

                DragAndDrop.AcceptDrag();
                this.favouritesService.AddFavourites(assetObjects);
                evt.StopPropagation();
            });
        }

        private static void OpenItem(FavouritesItem item)
        {
            if (!item.IsAsset)
            {
                return;
            }

            var obj = item.GetObject();
            if (obj != null)
            {
                AssetDatabase.OpenAsset(obj);
            }
        }

        private bool UpdateDropOverlay()
        {
            var hasValidAssets = DragAndDrop.objectReferences.Any(FavouritesService.CanAddFavourite);
            this.dropLabel.style.display = hasValidAssets ? DisplayStyle.Flex : DisplayStyle.None;
            return hasValidAssets;
        }

        private void OnDragExited(DragExitedEvent evt)
        {
            this.dropLabel.style.display = DisplayStyle.None;
        }

        private void OnOpenButtonClick(ClickEvent evt)
        {
            evt.StopPropagation();
        }

        private void OnFavouriteItemReordered(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < this.FilteredItems.Count &&
                toIndex >= 0 && toIndex < this.FilteredItems.Count)
            {
                this.favouritesService?.ReorderFavourite(fromIndex, toIndex);
            }
        }
    }
}
