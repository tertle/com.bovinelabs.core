// <copyright file="BaseObjectService.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.Base
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Utility;
    using Unity.Serialization.Editor;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Base class for services that manage object collections and provide preference access.
    /// </summary>
    /// <typeparam name="TItem">The type of object item.</typeparam>
    /// <typeparam name="TPreferences">The type of preferences.</typeparam>
    public abstract class BaseObjectService<TItem, TPreferences> : IDisposable
        where TItem : BaseObjectItem
        where TPreferences : BaseDisplayPreferences, new()
    {
        protected BaseObjectService(string preferenceKey)
        {
            this.Preferences = UserSettings<TPreferences>.GetOrCreate(preferenceKey);

            // Listen for preference changes
            this.Preferences.PreferencesChanged += this.OnPreferencesChanged;

            // Load persisted
            // ReSharper disable once VirtualMemberCallInConstructor
            this.Load();
        }

        /// <summary>Occurs when the collection changes.</summary>
        public event Action<IReadOnlyList<TItem>> ItemsChanged;

        /// <summary>Gets the current items as a read-only list.</summary>
        public abstract IReadOnlyList<TItem> Items { get; }

        /// <summary>Gets the item height.</summary>
        public int ItemHeight => this.Preferences.ItemHeight;

        /// <summary>Gets a value indicating whether to use a monospace font for the list.</summary>
        public bool UseMonospaceFont => this.Preferences.UseMonospaceFont;

        /// <summary>Gets a value indicating whether to show icons.</summary>
        public bool ShowIcons => this.Preferences.ShowIcons;

        /// <summary>Gets a value indicating whether to show timestamps.</summary>
        public bool ShowTimestamps => this.Preferences.ShowTimestamps;

        /// <summary>Gets a value indicating whether to show asset paths.</summary>
        public bool ShowAssetPaths => this.Preferences.ShowAssetPaths;

        /// <summary>Gets a value indicating whether to show type names in the list.</summary>
        public bool ShowTypeNames => this.Preferences.ShowTypeNames;

        /// <summary>Gets a value indicating whether to show the status bar.</summary>
        public bool ShowStatusBar => this.Preferences.ShowStatusBar;

        /// <summary>Gets a value indicating whether to grey out missing objects.</summary>
        public bool GreyOutMissingObjects => this.Preferences.GreyOutUnloadedObjects;

        /// <summary>Gets a value indicating whether to highlight currently selected objects.</summary>
        public bool HighlightCurrentSelection => this.Preferences.HighlightCurrentSelection;

        /// <summary>Gets the double-click threshold in seconds.</summary>
        public float DoubleClickThreshold => this.Preferences.DoubleClickThreshold;

        /// <summary>Gets the preferences instance for this service.</summary>
        protected TPreferences Preferences { get; }

        protected bool Disposed { get; private set;  }

        /// <summary>Cleans up resources.</summary>
        public virtual void Dispose()
        {
            if (this.Disposed)
            {
                return;
            }

            this.CleanupServices();
            this.Disposed = true;
        }

        /// <summary>Selects an object from history.</summary>
        /// <param name="item"> The item to select. </param>
        public void SelectItem(TItem item)
        {
            var obj = item.GetObject();
            if (obj == null)
            {
                // If the object no longer exists and it's not a scene object, remove it from history
                if (item.IsAsset)
                {
                    this.RemoveItem(item);
                }

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

                this.SelectFolder(item.GetObject()!);
            }
            else
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
            }
        }

        public void RemoveItem(TItem item)
        {
            if (this.TryRemoveItem(item))
            {
                this.Save();
                this.NotifyItemsChanged();
            }
        }

        /// <summary>Cleanup service-specific resources.</summary>
        protected virtual void CleanupServices()
        {
            this.Preferences.PreferencesChanged -= this.OnPreferencesChanged;

            this.Save();
        }

        /// <summary>Notify that items have changed.</summary>
        protected virtual void NotifyItemsChanged()
        {
            this.ItemsChanged?.Invoke(this.Items);
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
            private readonly List<Object> allObjects = Resources.FindObjectsOfTypeAll<Object>().ToList();
            private readonly Dictionary<GlobalObjectId, Object> objectsById = new();

            public Object TryGetObject(SerializableObjectItem item, out GlobalObjectId objectId)
            {
                objectId = ParseObjectId(item.GlobalIdString);
                if (!BaseObjectItem.HasValidObjectId(objectId))
                {
                    return null;
                }

                if (this.objectsById.TryGetValue(objectId, out var loadedObject))
                {
                    return loadedObject;
                }

                var index = 0;

                try
                {
                    for (index = 0; index < this.allObjects.Count; index++)
                    {
                        var obj = this.allObjects[index];
                        if (obj == null)
                        {
                            continue;
                        }

                        var loadedObjectId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                        if (!BaseObjectItem.HasValidObjectId(loadedObjectId))
                        {
                            continue;
                        }

                        this.objectsById.TryAdd(loadedObjectId, obj);

                        if (loadedObjectId.Equals(objectId))
                        {
                            index++;
                            return obj;
                        }
                    }
                }
                finally
                {
                    this.allObjects.RemoveRange(0, index);
                }

                return null;
            }

            public static DateTime GetTimestamp(SerializableObjectItem item)
            {
                return DateTime.FromBinary(item.Timestamp);
            }

            public static Texture2D GetIcon(Object obj)
            {
                return obj == null ? null : AssetPreview.GetMiniThumbnail(obj);
            }
        }

        /// <summary>Removes a specific history item.</summary>
        /// <param name="item"> The item to remove. </param>
        /// <returns> True if an item was removed. </returns>
        protected abstract bool TryRemoveItem(TItem item);

        protected virtual void SelectFolder(Object obj)
        {
        }

        protected abstract void Save();

        protected abstract void Load();

        private void OnPreferencesChanged()
        {
            // Notify that history has changed to trigger UI refresh
            this.NotifyItemsChanged();
        }

        private static GlobalObjectId ParseObjectId(string globalIdString)
        {
            GlobalObjectId.TryParse(globalIdString, out var objectId);
            return objectId;
        }
    }
}
