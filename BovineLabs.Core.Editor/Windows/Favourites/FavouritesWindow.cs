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

    public sealed class FavouritesWindow : BaseObjectWindow<FavouritesItem, FavouritesService, FavouritesPreferences>
    {
        private FavouritesService _favouritesService;
        private VisualElement _dropLabel;
        private Button _addSelectionButton;

        protected override FavouritesService Service => _favouritesService ?? FavouritesService.Instance;

        protected override string StylesheetPath => "Packages/com.bovinelabs.core/BovineLabs.Core.Editor/Windows/Favourites/FavouritesWindow.uss";

        protected override string RootClassName => "favourites-window";

        protected override GUIContent WindowTitle => new("Favourites", EditorGUIUtility.IconContent("Favorite Icon").image);

        [MenuItem(EditorMenus.RootMenuTools + "Favourites Window", priority = 1021)]
        private static void ShowWindow()
        {
            var window = GetWindow<FavouritesWindow>();
            window.titleContent = window.WindowTitle;
            window.Show();
        }

        protected override void InitializeServices()
        {
            _favouritesService = FavouritesService.Instance;
            _favouritesService.ItemsChanged += OnItemsChangedInternal;
        }

        protected override void CleanupServices()
        {
            if (_favouritesService != null)
            {
                _favouritesService.ItemsChanged -= OnItemsChangedInternal;
            }
        }

        protected override void SetupAdditionalFeatures(VisualElement root)
        {
            SetupDragAndDrop(root);
        }

        protected override void OnItemsChanged(IReadOnlyList<FavouritesItem> items)
        {
        }

        protected override void UpdateItemsVisualState()
        {
            base.UpdateItemsVisualState();
            UpdateAddSelectionButton();
        }

        protected override void CreateCustomToolbarElements(Toolbar toolbar)
        {
            _addSelectionButton = new ToolbarButton(() => _favouritesService.AddFavourites(Selection.objects))
            {
                text = "Add Selection",
                tooltip = "Add selected assets to favourites",
            };

            toolbar.Add(_addSelectionButton);
            UpdateAddSelectionButton();
        }

        protected override Button CreateListItemActionButton()
        {
            var openButton = new Button();
            openButton.AddToClassList("favourite-item-open-button");
            openButton.tooltip = "Open object";
            return openButton;
        }

        protected override void BindListItem(VisualElement element, int index)
        {
            if (index >= FilteredItems.Count)
            {
                return;
            }

            var item = FilteredItems[index];
            BindListItemCommon(element, item);

            // Handle open button
            var openButton = element.Q<Button>();
            if (openButton != null)
            {
                openButton.SetEnabled(item.IsAsset);
                BindButtonClickAction(openButton, () => OpenItem(item), OnOpenButtonClick);
            }
        }

        protected override void OnListItemClicked(FavouritesItem item)
        {
            _favouritesService?.SelectFromFavourites(item);
        }

        protected override void OnListItemDoubleClicked(FavouritesItem item)
        {
            OpenItem(item);
        }

        protected override void CreateContextMenu(ContextualMenuPopulateEvent evt, FavouritesItem item)
        {
            evt.menu.AppendAction("Select Object", _ => _favouritesService?.SelectFromFavourites(item));

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
                    if (_favouritesService?.ConfirmRemoval == true)
                    {
                        if (!EditorUtility.DisplayDialog("Remove Favourite", $"Remove '{item.Name}' from favourites?", "Remove", "Cancel"))
                        {
                            return;
                        }
                    }

                    _favouritesService?.RemoveItem(item);
                });
        }

        protected override string GetStatusText(int totalCount, int filteredCount, int aliveCount)
        {
            return filteredCount == totalCount
                ? $"{totalCount} favourites ({aliveCount} alive)"
                : $"{filteredCount} of {totalCount} favourites ({aliveCount} alive)";
        }

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

                _favouritesService?.ClearFavourites();
            });
        }

        protected override void CreateSettingsMenu()
        {
            CreateStandardSettingsMenu(FavouritesService.PreferenceKey);
        }

        protected override bool IsReorderable() => !IsFiltered();

        protected override void SetupListViewCallbacks()
        {
            MainListView.reorderMode = ListViewReorderMode.Animated;
            MainListView.itemIndexChanged += OnFavouriteItemReordered;
        }

        protected override void PostProcessFilteredItems()
        {
            // Update reorderable state
            MainListView!.reorderable = !IsFiltered();
            UpdateAddSelectionButton();
        }

        protected override string GetTimestampFormat() => "yyyy-MM-dd HH:mm";

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
            return !string.IsNullOrEmpty(CurrentSearchText) || CurrentTypeFilter != "All";
        }

        private void UpdateAddSelectionButton()
        {
            _addSelectionButton.SetEnabled(Selection.objects.Any(obj =>
                FavouritesService.CanAddFavourite(obj) && !_favouritesService.IsFavourite(obj)));
        }

        private void SetupDragAndDrop(VisualElement root)
        {
            _dropLabel = new VisualElement { pickingMode = PickingMode.Ignore };
            _dropLabel.AddToClassList("favourites-drop-overlay");
            _dropLabel.RegisterCallback<AttachToPanelEvent>(evt =>
                evt.destinationPanel.visualTree.RegisterCallback<DragExitedEvent>(OnDragExited));
            _dropLabel.RegisterCallback<DetachFromPanelEvent>(evt =>
                evt.originPanel.visualTree.UnregisterCallback<DragExitedEvent>(OnDragExited));

            root.Add(_dropLabel);

            root.RegisterCallback<DragEnterEvent>(_ =>
            {
                UpdateDropOverlay();
            });

            root.RegisterCallback<DragLeaveEvent>(_ =>
            {
                _dropLabel.style.display = DisplayStyle.None;
            });

            root.RegisterCallback<DragUpdatedEvent>(evt =>
            {
                if (DragAndDrop.objectReferences.Length == 0)
                {
                    return;
                }

                var hasValidAssets = UpdateDropOverlay();
                DragAndDrop.visualMode = hasValidAssets ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Rejected;
                evt.StopPropagation();
            });

            root.RegisterCallback<DragPerformEvent>(evt =>
            {
                _dropLabel.style.display = DisplayStyle.None;

                var assetObjects = DragAndDrop.objectReferences.Where(FavouritesService.CanAddFavourite).ToArray();
                if (assetObjects.Length == 0)
                {
                    return;
                }

                DragAndDrop.AcceptDrag();
                _favouritesService.AddFavourites(assetObjects);
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
            _dropLabel.style.display = hasValidAssets ? DisplayStyle.Flex : DisplayStyle.None;
            return hasValidAssets;
        }

        private void OnDragExited(DragExitedEvent evt)
        {
            _dropLabel.style.display = DisplayStyle.None;
        }

        private void OnOpenButtonClick(ClickEvent evt)
        {
            evt.StopPropagation();
        }

        private void OnFavouriteItemReordered(int fromIndex, int toIndex)
        {
            if (fromIndex >= 0 && fromIndex < FilteredItems.Count &&
                toIndex >= 0 && toIndex < FilteredItems.Count)
            {
                _favouritesService?.ReorderFavourite(fromIndex, toIndex);
            }
        }
    }
}
