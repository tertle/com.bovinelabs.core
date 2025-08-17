// <copyright file="FavouritesService.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.Favourites
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Windows.Base;
    using UnityEditor;
    using UnityEngine;
    using Object = UnityEngine.Object;

    /// <summary>
    /// Service that manages favourites collection in Unity Editor.
    /// </summary>
    public sealed class FavouritesService : BaseObjectService<FavouritesItem, FavouritesPreferences>
    {
        public const string PreferenceKey = "Favourites";

        private static FavouritesService? instance;

        private readonly List<FavouritesItem> favourites = new();

        private FavouritesService()
            : base(PreferenceKey)
        {
        }

        /// <summary>Gets the current favourites as a read-only list.</summary>
        public override IReadOnlyList<FavouritesItem> Items => new List<FavouritesItem>(this.favourites);

        /// <summary>Gets the singleton instance of the favourites service.</summary>
        public static FavouritesService Instance
        {
            get
            {
                if (instance == null || instance.Disposed)
                {
                    instance = new FavouritesService();
                }

                return instance;
            }
        }

        /// <summary>Gets a value indicating whether to confirm removal.</summary>
        public bool ConfirmRemoval => this.Preferences.ConfirmRemoval;

        /// <summary>Adds an object to favourites.</summary>
        /// <param name="obj"> The object to add. </param>
        /// <returns> True if added, false if already exists. </returns>
        public bool AddFavourite(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var objectId = GlobalObjectId.GetGlobalObjectIdSlow(obj);

            // Check if already exists
            var existingIndex = this.favourites.FindIndex(f => f.GlobalId.Equals(objectId));
            if (existingIndex >= 0)
            {
                return false; // Already exists
            }

            // Add new favourite
            var favouriteItem = new FavouritesItem(obj);
            this.favourites.Add(favouriteItem);

            this.Save();
            this.NotifyItemsChanged();
            return true;
        }

        /// <summary>Adds multiple objects to favourites.</summary>
        /// <param name="objects"> The objects to add. </param>
        /// <returns> Number of objects successfully added. </returns>
        public int AddFavourites(IEnumerable<Object> objects)
        {
            var addedCount = 0;
            var hasChanges = false;

            var o = objects.ToArray();

            var objectIds = new GlobalObjectId[o.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(o, objectIds);

            for (var index = 0; index < o.Length; index++)
            {
                var obj = o[index];
                if (obj == null)
                {
                    continue;
                }

                var objectId = objectIds[index];

                // Check if already exists
                if (this.favourites.Any(f => f.GlobalId.Equals(objectId)))
                {
                    continue;
                }

                // Add new favourite
                var favouriteItem = new FavouritesItem(obj);
                this.favourites.Add(favouriteItem);
                addedCount++;
                hasChanges = true;
            }

            if (hasChanges)
            {
                this.Save();
                this.NotifyItemsChanged();
            }

            return addedCount;
        }

        /// <summary>Removes an object from favourites by object reference.</summary>
        /// <param name="obj"> The object to remove. </param>
        /// <returns> True if removed, false if not found. </returns>
        public bool RemoveFavourite(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var objectId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            var index = this.favourites.FindIndex(f => f.GlobalId.Equals(objectId));

            if (index >= 0)
            {
                this.favourites.RemoveAt(index);
                this.Save();
                this.NotifyItemsChanged();
                return true;
            }

            return false;
        }

        /// <summary>Clears all favourites.</summary>
        public void ClearFavourites()
        {
            if (this.favourites.Count > 0)
            {
                this.favourites.Clear();
                this.Save();
                this.NotifyItemsChanged();
            }
        }

        /// <summary>Checks if an object is already in favourites.</summary>
        /// <param name="obj"> The object to check. </param>
        /// <returns> True if in favourites, false otherwise. </returns>
        public bool IsFavourite(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var objectId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            return this.favourites.Any(f => f.GlobalId.Equals(objectId));
        }

        /// <summary>Reorders a favourite item to a new position relative to another item.</summary>
        /// <param name="itemToMove"> The item to move. </param>
        /// <param name="targetItem"> The item to move it relative to. </param>
        public void ReorderFavourite(FavouritesItem itemToMove, FavouritesItem targetItem)
        {
            var oldIndex = this.favourites.IndexOf(itemToMove);
            var newIndex = this.favourites.IndexOf(targetItem);

            if (oldIndex == -1 || newIndex == -1 || oldIndex == newIndex)
            {
                return;
            }

            // Simple swap of the two items
            this.favourites[oldIndex] = targetItem;
            this.favourites[newIndex] = itemToMove;

            this.Save();
            this.NotifyItemsChanged();
        }

        /// <summary>Selects an object from favourites.</summary>
        /// <param name="item"> The item to select. </param>
        public void SelectFromFavourites(FavouritesItem item)
        {
            var obj = item.GetObject();
            if (obj == null)
            {
                return;
            }

            Selection.activeObject = obj;

            // If it's a scene object, also ping it
            if (!item.IsAsset)
            {
                EditorGUIUtility.PingObject(obj);
            }
        }

        /// <inheritdoc/>
        protected override bool TryRemoveItem(FavouritesItem item)
        {
            return this.favourites.Remove(item);
        }

        /// <inheritdoc/>
        protected override void Save()
        {
            try
            {
                this.Preferences.FavouritesData.Clear();

                foreach (var item in this.favourites)
                {
                    var serializableItem = new SerializableFavouriteItem
                    {
                        Name = item.Name,
                        TypeName = item.TypeName,
                        AssetPath = item.AssetPath,
                        Timestamp = item.Timestamp.ToBinary(),
                        GlobalIdString = item.GlobalId.ToString(),
                        Icon = GlobalObjectId.GetGlobalObjectIdSlow(item.Icon).ToString(),
                    };

                    this.Preferences.FavouritesData.Add(serializableItem);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to save favourites: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        protected override void Load()
        {
            try
            {
                if (this.Preferences.FavouritesData.Count == 0)
                {
                    return;
                }

                var pathCache = new Dictionary<string, Object>();
                var allObjects = Resources.FindObjectsOfTypeAll<Object>().ToList();

                foreach (var item in this.Preferences.FavouritesData)
                {
                    // Create favourite item
                    GlobalObjectId.TryParse(item.GlobalIdString, out var savedGlobalId);
                    var obj = TryGetAssetIfLoaded(item.AssetPath, allObjects, pathCache);

                    var timestamp = DateTime.FromBinary(item.Timestamp);

                    GlobalObjectId.TryParse(item.Icon, out var iconId);
                    var icon = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(iconId) as Texture2D;

                    var favouriteItem = new FavouritesItem(obj, item.Name, item.TypeName, item.AssetPath, savedGlobalId, icon, timestamp);
                    this.favourites.Add(favouriteItem);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load favourites: {ex.Message}");
            }
        }
    }
}
