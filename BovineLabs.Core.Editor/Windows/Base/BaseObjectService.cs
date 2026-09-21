namespace BovineLabs.Core.Editor.Windows.Base
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Utility;
    using Unity.Entities.Editor.Serialization;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public abstract class BaseObjectService<TItem, TPreferences> : IDisposable
        where TItem : BaseObjectItem
        where TPreferences : BaseDisplayPreferences, new()
    {
        protected BaseObjectService(string preferenceKey)
        {
            Preferences = UserSettings<TPreferences>.GetOrCreate(preferenceKey);

            // Listen for preference changes
            Preferences.PreferencesChanged += OnPreferencesChanged;

            // Load persisted
            // ReSharper disable once VirtualMemberCallInConstructor
            Load();
        }

        public event Action<IReadOnlyList<TItem>> ItemsChanged;

        public abstract IReadOnlyList<TItem> Items { get; }

        public int ItemHeight => Preferences.ItemHeight;

        public bool UseMonospaceFont => Preferences.UseMonospaceFont;

        public bool ShowIcons => Preferences.ShowIcons;

        public bool ShowTimestamps => Preferences.ShowTimestamps;

        public bool ShowAssetPaths => Preferences.ShowAssetPaths;

        public bool ShowTypeNames => Preferences.ShowTypeNames;

        public bool ShowStatusBar => Preferences.ShowStatusBar;

        public bool GreyOutMissingObjects => Preferences.GreyOutUnloadedObjects;

        public bool HighlightCurrentSelection => Preferences.HighlightCurrentSelection;

        /// <summary>
        /// Threshold in seconds.
        /// </summary>
        public float DoubleClickThreshold => Preferences.DoubleClickThreshold;

        protected TPreferences Preferences { get; }

        protected bool Disposed { get; private set; }

        public virtual void Dispose()
        {
            if (Disposed)
            {
                return;
            }

            CleanupServices();
            Disposed = true;
        }

        public virtual void SelectItem(TItem item)
        {
            var obj = item.GetObject();
            if (obj == null)
            {
                return;
            }

            // If it's a folder asset, open it in the project window
            if (item.IsAsset && AssetDatabase.IsValidFolder(item.AssetPath))
            {
                var browsers = Resources.FindObjectsOfTypeAll(ProjectView.Internal.ProjectBrowserType);
                foreach (var projectBrowser in browsers)
                {
                    ProjectView.Internal.EndPing(projectBrowser);
                }

                foreach (var projectBrowser in browsers)
                {
                    ProjectView.Internal.ShowFolderContents(projectBrowser, item.AssetPath);
                }

                SelectFolder(obj);
            }
            else
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }

        public void RemoveItem(TItem item)
        {
            if (TryRemoveItem(item))
            {
                Save();
                NotifyItemsChanged();
            }
        }

        protected virtual void CleanupServices()
        {
            Preferences.PreferencesChanged -= OnPreferencesChanged;

            Save();
        }

        protected virtual void NotifyItemsChanged()
        {
            ItemsChanged?.Invoke(Items);
        }

        protected static List<TSerializable> CreateSerializableItems<TObjectItem, TSerializable>(IReadOnlyList<TObjectItem> items)
            where TObjectItem : BaseObjectItem
            where TSerializable : SerializableObjectItem, new()
        {
            return CreateSerializableItems<TObjectItem, TSerializable>(items, 0, items.Count, null);
        }

        protected static List<TSerializable> CreateSerializableItems<TObjectItem, TSerializable>(
            IReadOnlyList<TObjectItem> items, int startIndex, int count, Action<TObjectItem, TSerializable> configure)
            where TObjectItem : BaseObjectItem
            where TSerializable : SerializableObjectItem, new()
        {
            var data = new List<TSerializable>(count);
            for (var index = 0; index < count; index++)
            {
                var item = items[startIndex + index];
                item.RefreshMetadata();
                if (!BaseObjectItem.HasValidObjectId(item.GlobalId))
                {
                    item.RefreshIdentity();
                }

                // Unsaved scene and temporary objects have no identity that can survive an Editor restart.
                if (!BaseObjectItem.HasValidObjectId(item.GlobalId) && !item.IsAsset)
                {
                    continue;
                }

                var serializableItem = new TSerializable
                {
                    Name = item.Name,
                    TypeName = item.TypeName,
                    AssetPath = item.AssetPath,
                    Timestamp = item.Timestamp.ToBinary(),
                    GlobalIdString = item.GlobalId.ToString(),
                    Icon = string.Empty,
                };

                configure?.Invoke(item, serializableItem);
                data.Add(serializableItem);
            }

            return data;
        }

        protected sealed class LoadedObjectLookup
        {
            private readonly List<Object> _allObjects = Resources.FindObjectsOfTypeAll<Object>().ToList();
            private readonly Dictionary<GlobalObjectId, Object> _objectsById = new();

            public Object TryGetObject(SerializableObjectItem item, out GlobalObjectId objectId)
            {
                objectId = ParseObjectId(item.GlobalIdString);
                if (!BaseObjectItem.HasValidObjectId(objectId))
                {
                    return null;
                }

                if (_objectsById.TryGetValue(objectId, out var loadedObject))
                {
                    return loadedObject;
                }

                var index = 0;

                try
                {
                    for (index = 0; index < _allObjects.Count; index++)
                    {
                        var obj = _allObjects[index];
                        if (obj == null)
                        {
                            continue;
                        }

                        var loadedObjectId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                        if (!BaseObjectItem.HasValidObjectId(loadedObjectId))
                        {
                            continue;
                        }

                        _objectsById.TryAdd(loadedObjectId, obj);

                        if (loadedObjectId.Equals(objectId))
                        {
                            index++;
                            return obj;
                        }
                    }
                }
                finally
                {
                    _allObjects.RemoveRange(0, index);
                }

                return null;
            }

            public static bool TryGetTimestamp(SerializableObjectItem item, out DateTime timestamp)
            {
                timestamp = default;
                if (item == null)
                {
                    BLGlobalLogger.LogWarningString("Ignoring an empty saved object window entry.");
                    return false;
                }

                try
                {
                    timestamp = DateTime.FromBinary(item.Timestamp);
                    return true;
                }
                catch (ArgumentException)
                {
                    BLGlobalLogger.LogWarningString($"Ignoring saved object window entry '{item.Name}' with an invalid timestamp.");
                    return false;
                }
            }

            public static Texture2D GetIcon(Object obj)
            {
                return obj == null ? null : AssetPreview.GetMiniThumbnail(obj);
            }
        }

        protected abstract bool TryRemoveItem(TItem item);

        protected virtual void SelectFolder(Object obj)
        {
            Selection.activeObject = obj;
        }

        protected abstract void Save();

        protected abstract void Load();

        protected virtual void OnPreferencesChanged()
        {
            // Notify that history has changed to trigger UI refresh
            NotifyItemsChanged();
        }

        private static GlobalObjectId ParseObjectId(string globalIdString)
        {
            GlobalObjectId.TryParse(globalIdString, out var objectId);
            return objectId;
        }
    }
}
