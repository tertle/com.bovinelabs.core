// <copyright file="SelectionHistoryService.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.SelectionHistory
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Windows.Base;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Service that manages selection history tracking in Unity Editor.
    /// </summary>
    public sealed class SelectionHistoryService : BaseObjectService<SelectionHistoryItem, SelectionHistoryPreferences>
    {
        public const string PreferenceKey = "Selection History";

        private static SelectionHistoryService instance;

        private readonly List<SelectionHistoryItem> allItems = new();
        private readonly List<SelectionHistoryItem> lockedItems = new();
        private readonly List<SelectionHistoryItem> normalItems = new();

        private SelectionHistoryService()
            : base(PreferenceKey)
        {
            Selection.selectionChanged += this.OnSelectionChanged;
        }

        /// <summary>Gets the current history as a read-only list.</summary>
        public override IReadOnlyList<SelectionHistoryItem> Items => this.allItems;

        /// <summary>Gets the locked items as a read-only list.</summary>
        public IReadOnlyList<SelectionHistoryItem> LockedItems => this.lockedItems;

        /// <summary>Gets the normal items as a read-only list.</summary>
        public IReadOnlyList<SelectionHistoryItem> NormalItems => this.normalItems;

        /// <summary>Gets the singleton instance of the selection history service.</summary>
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

        /// <summary>Gets the maximum number of items to keep in history.</summary>
        public int MaxHistorySize => this.Preferences.MaxHistorySize;

        /// <summary>Clears all unlocked history items.</summary>
        public void ClearHistory()
        {
            this.normalItems.Clear();
            this.RebuildItems();
            this.Save();
            this.NotifyItemsChanged();
        }

        /// <summary>Toggles the locked state of a history item.</summary>
        /// <param name="item"> The item to lock. </param>
        public void ToggleLock(SelectionHistoryItem item)
        {
            item.IsLocked = !item.IsLocked;

            // Move item between collections based on new locked state
            if (item.IsLocked)
            {
                if (this.normalItems.Remove(item))
                {
                    this.lockedItems.Add(item);
                }
            }
            else
            {
                if (this.lockedItems.Remove(item))
                {
                    this.normalItems.Add(item);
                }
            }

            this.RebuildItems();
            this.Save();
            this.NotifyItemsChanged();
        }

        /// <summary>Reorders a locked item to a new position.</summary>
        /// <param name="fromIndex">The current index of the item.</param>
        /// <param name="toIndex">The target index for the item.</param>
        public void ReorderLockedItem(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= this.lockedItems.Count ||
                toIndex < 0 || toIndex >= this.lockedItems.Count ||
                fromIndex == toIndex)
            {
                return;
            }

            var item = this.lockedItems[fromIndex];
            this.lockedItems.RemoveAt(fromIndex);
            this.lockedItems.Insert(toIndex, item);

            this.RebuildItems();
            this.Save();
            this.NotifyItemsChanged();
        }

        /// <summary>Cleans up service-specific resources.</summary>
        protected override void CleanupServices()
        {
            base.CleanupServices();

            Selection.selectionChanged -= this.OnSelectionChanged;
        }

        /// <inheritdoc/>
        protected override bool TryRemoveItem(SelectionHistoryItem item)
        {
            var changed = this.lockedItems.Remove(item);
            changed |= this.normalItems.Remove(item);
            if (changed)
            {
                this.RebuildItems();
            }

            return changed;
        }

        /// <inheritdoc/>
        protected override void SelectFolder(Object folder)
        {
            this.SelectObject(folder);
        }

        /// <inheritdoc/>
        protected override void Save()
        {
            try
            {
                var lockedData = CreateSerializableItems<SelectionHistoryItem, SerializableHistoryItem>(this.lockedItems, 0, this.lockedItems.Count, SetLocked);

                // Save normal items (respect max history size)
                var normalCount = Math.Min(this.normalItems.Count, this.MaxHistorySize);
                var normalData = CreateSerializableItems<SelectionHistoryItem, SerializableHistoryItem>(
                    this.normalItems, this.normalItems.Count - normalCount, normalCount, SetLocked);

                this.Preferences.LockedHistoryData = lockedData;
                this.Preferences.NormalHistoryData = normalData;
            }
            catch (Exception ex)
            {
                BLGlobalLogger.LogWarningString($"Failed to save selection history: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        protected override void Load()
        {
            try
            {
                if (this.Preferences.LockedHistoryData.Count == 0 && this.Preferences.NormalHistoryData.Count == 0)
                {
                    return;
                }

                var loadedObjects = new LoadedObjectLookup();

                // Load locked items
                foreach (var item in this.Preferences.LockedHistoryData)
                {
                    var obj = loadedObjects.TryGetObject(item, out var savedGlobalId);
                    var timestamp = LoadedObjectLookup.GetTimestamp(item);
                    var icon = LoadedObjectLookup.GetIcon(obj);

                    var historyItem = new SelectionHistoryItem(obj, item.Name, item.TypeName, item.AssetPath, savedGlobalId, icon, timestamp, item.IsLocked);
                    this.lockedItems.Add(historyItem);
                }

                // Load normal items
                foreach (var item in this.Preferences.NormalHistoryData)
                {
                    var obj = loadedObjects.TryGetObject(item, out var savedGlobalId);
                    var timestamp = LoadedObjectLookup.GetTimestamp(item);
                    var icon = LoadedObjectLookup.GetIcon(obj);

                    var historyItem = new SelectionHistoryItem(obj, item.Name, item.TypeName, item.AssetPath, savedGlobalId, icon, timestamp, item.IsLocked);
                    this.normalItems.Add(historyItem);
                }

                this.RebuildItems();
            }
            catch (Exception ex)
            {
                BLGlobalLogger.LogWarningString($"Failed to load selection history: {ex.Message}");
            }
        }

        private void OnSelectionChanged()
        {
            if (this.Disposed)
            {
                return;
            }

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

            // Check if we should track scene objects
            if (!this.Preferences.TrackSceneObjects)
            {
                var assetPath = AssetDatabase.GetAssetPath(activeObject);
                if (string.IsNullOrEmpty(assetPath))
                {
                    // This is a scene object and we're not tracking scene objects, so skip it
                    return;
                }
            }

            this.SelectObject(activeObject);
        }

        private void SelectObject(Object activeObject)
        {
            var objectId = GlobalObjectId.GetGlobalObjectIdSlow(activeObject);

            // Check if this object is already in locked items - if so, don't move it
            var existingLockedItem = this.lockedItems.FirstOrDefault(item => item.GlobalId.Equals(objectId));
            if (existingLockedItem != null)
            {
                // Object is locked, don't move it - but still notify to refresh visual state
                this.NotifyItemsChanged();
                return;
            }

            // Find existing entry in normal items
            var existingNormalIndex = -1;
            for (int i = 0; i < this.normalItems.Count; i++)
            {
                if (this.normalItems[i].GlobalId.Equals(objectId))
                {
                    existingNormalIndex = i;
                    break;
                }
            }

            // If found in normal items, remove it (it will be re-added at the end)
            if (existingNormalIndex >= 0)
            {
                this.normalItems.RemoveAt(existingNormalIndex);
            }

            // Add the item to the end of normal items (most recent)
            var historyItem = new SelectionHistoryItem(activeObject, objectId, false);
            this.normalItems.Add(historyItem);

            // Trim normal history if it exceeds max size (locked items don't count against the limit)
            while (this.normalItems.Count > this.MaxHistorySize)
            {
                this.normalItems.RemoveAt(0);
            }

            this.RebuildItems();
            this.Save();
            this.NotifyItemsChanged();
        }

        private void RebuildItems()
        {
            this.allItems.Clear();
            this.allItems.AddRange(this.lockedItems);
            this.allItems.AddRange(this.normalItems);
        }

        private static void SetLocked(SelectionHistoryItem item, SerializableHistoryItem serializableItem)
        {
            serializableItem.IsLocked = item.IsLocked;
        }
    }
}
