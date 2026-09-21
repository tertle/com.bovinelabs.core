namespace BovineLabs.Core.Editor.Windows.SelectionHistory
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Windows.Base;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using UnityEditor.SceneManagement;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Object = UnityEngine.Object;

    public sealed class SelectionHistoryService : BaseObjectService<SelectionHistoryItem, SelectionHistoryPreferences>
    {
        public const string PreferenceKey = "Selection History";

        [NoAutoStaticsCleanup]
        private static SelectionHistoryService instance;

        private readonly List<SelectionHistoryItem> _allItems = new();
        private readonly List<SelectionHistoryItem> _lockedItems = new();
        private readonly List<SelectionHistoryItem> _normalItems = new();
        private Object _replayedObject;

        private SelectionHistoryService()
            : base(PreferenceKey)
        {
            Selection.selectionChanged += OnSelectionChanged;
            EditorSceneManager.sceneSaved += OnSceneSaved;
        }

        public override IReadOnlyList<SelectionHistoryItem> Items => _allItems;

        public IReadOnlyList<SelectionHistoryItem> LockedItems => _lockedItems;

        public IReadOnlyList<SelectionHistoryItem> NormalItems => _normalItems;

        public static SelectionHistoryService Instance
        {
            get
            {
                if (instance == null || instance.Disposed)
                {
                    instance = new SelectionHistoryService();
                }

                return instance;
            }
        }

        public int MaxHistorySize => Preferences.MaxHistorySize;

        public override void SelectItem(SelectionHistoryItem item)
        {
            // Selection notifications can be deferred, so retain the object until the next change is received.
            // Replaying history leaves the clicked row in place, including for a second click to open it.
            _replayedObject = item.GetObject();
            base.SelectItem(item);
        }

        public void ClearHistory()
        {
            _normalItems.Clear();
            RebuildItems();
            Save();
            NotifyItemsChanged();
        }

        public void ToggleLock(SelectionHistoryItem item)
        {
            item.IsLocked = !item.IsLocked;

            // Move item between collections based on new locked state
            if (item.IsLocked)
            {
                if (_normalItems.Remove(item))
                {
                    _lockedItems.Add(item);
                }
            }
            else
            {
                if (_lockedItems.Remove(item))
                {
                    _normalItems.Add(item);
                }
            }

            RebuildItems();
            Save();
            NotifyItemsChanged();
        }

        public void ReorderLockedItem(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= _lockedItems.Count ||
                toIndex < 0 || toIndex >= _lockedItems.Count ||
                fromIndex == toIndex)
            {
                return;
            }

            var item = _lockedItems[fromIndex];
            _lockedItems.RemoveAt(fromIndex);
            _lockedItems.Insert(toIndex, item);

            RebuildItems();
            Save();
            NotifyItemsChanged();
        }

        protected override void CleanupServices()
        {
            base.CleanupServices();

            Selection.selectionChanged -= OnSelectionChanged;
            EditorSceneManager.sceneSaved -= OnSceneSaved;
        }

        protected override bool TryRemoveItem(SelectionHistoryItem item)
        {
            var changed = _lockedItems.Remove(item);
            changed |= _normalItems.Remove(item);
            if (changed)
            {
                RebuildItems();
            }

            return changed;
        }

        protected override void OnPreferencesChanged()
        {
            RebuildItems();
            Save();
            base.OnPreferencesChanged();
        }

        protected override void Save()
        {
            Preferences.LockedHistoryData = CreateSerializableItems<SelectionHistoryItem, SerializableHistoryItem>(
                _lockedItems, 0, _lockedItems.Count, SetLocked);
            Preferences.NormalHistoryData = CreateSerializableItems<SelectionHistoryItem, SerializableHistoryItem>(
                _normalItems, 0, _normalItems.Count, SetLocked);
        }

        protected override void Load()
        {
            if (Preferences.LockedHistoryData.Count == 0 && Preferences.NormalHistoryData.Count == 0)
            {
                return;
            }

            var loadedObjects = new LoadedObjectLookup();

            foreach (var item in Preferences.LockedHistoryData)
            {
                if (!LoadedObjectLookup.TryGetTimestamp(item, out var timestamp))
                {
                    continue;
                }

                var obj = loadedObjects.TryGetObject(item, out var savedGlobalId);
                var icon = LoadedObjectLookup.GetIcon(obj);
                var historyItem = new SelectionHistoryItem(obj, item.Name, item.TypeName, item.AssetPath, savedGlobalId, icon, timestamp, true);
                _lockedItems.Add(historyItem);
            }

            foreach (var item in Preferences.NormalHistoryData)
            {
                if (!LoadedObjectLookup.TryGetTimestamp(item, out var timestamp))
                {
                    continue;
                }

                var obj = loadedObjects.TryGetObject(item, out var savedGlobalId);
                var icon = LoadedObjectLookup.GetIcon(obj);
                var historyItem = new SelectionHistoryItem(obj, item.Name, item.TypeName, item.AssetPath, savedGlobalId, icon, timestamp, false);
                _normalItems.Add(historyItem);
            }

            RebuildItems();
        }

        private void OnSceneSaved(Scene scene)
        {
            foreach (var item in _allItems)
            {
                var gameObject = item.ObjectRef.Target as GameObject;
                if (gameObject == null && item.ObjectRef.Target is Component component && component != null)
                {
                    gameObject = component.gameObject;
                }

                if (gameObject != null && gameObject.scene == scene)
                {
                    item.RefreshIdentity();
                }
            }

            Save();
            NotifyItemsChanged();
        }

        private void OnSelectionChanged()
        {
            if (Disposed)
            {
                return;
            }

            var replayedSelection = _replayedObject;
            _replayedObject = null;

            var objs = Selection.objects;
            if (objs is { Length: > 1 })
            {
                // If we are multi selecting, history is a confusing so just ignore it
                return;
            }

            Object activeObject = Selection.activeGameObject;
            if (activeObject == null)
            {
                activeObject = Selection.activeObject;
                if (activeObject == null)
                {
                    return;
                }
            }

            if (replayedSelection != null && (activeObject == replayedSelection || Selection.activeObject == replayedSelection))
            {
                return;
            }

            // Check if we should track scene objects
            if (!Preferences.TrackSceneObjects)
            {
                var assetPath = AssetDatabase.GetAssetPath(activeObject);
                if (string.IsNullOrEmpty(assetPath))
                {
                    // This is a scene object and we're not tracking scene objects, so skip it
                    return;
                }
            }

            SelectObject(activeObject);
        }

        private void SelectObject(Object activeObject)
        {
            var objectId = GlobalObjectId.GetGlobalObjectIdSlow(activeObject);

            // Check if this object is already in locked items - if so, don't move it
            var existingLockedItem = _lockedItems.FirstOrDefault(item => item.MatchesObject(activeObject, objectId));
            if (existingLockedItem != null)
            {
                // Object is locked, don't move it - but still notify to refresh visual state
                NotifyItemsChanged();
                return;
            }

            // Find existing entry in normal items
            var existingNormalIndex = -1;
            for (int i = 0; i < _normalItems.Count; i++)
            {
                if (_normalItems[i].MatchesObject(activeObject, objectId))
                {
                    existingNormalIndex = i;
                    break;
                }
            }

            // If found in normal items, remove it (it will be re-added at the end)
            if (existingNormalIndex >= 0)
            {
                _normalItems.RemoveAt(existingNormalIndex);
            }

            // Add the item to the end of normal items (most recent)
            var historyItem = new SelectionHistoryItem(activeObject, objectId, false);
            _normalItems.Add(historyItem);

            RebuildItems();
            Save();
            NotifyItemsChanged();
        }

        private void RebuildItems()
        {
            var excessCount = _normalItems.Count - MaxHistorySize;
            if (excessCount > 0)
            {
                _normalItems.RemoveRange(0, excessCount);
            }

            _allItems.Clear();
            _allItems.AddRange(_lockedItems);
            _allItems.AddRange(_normalItems);
        }

        private static void SetLocked(SelectionHistoryItem item, SerializableHistoryItem serializableItem)
        {
            serializableItem.IsLocked = item.IsLocked;
        }
    }
}
