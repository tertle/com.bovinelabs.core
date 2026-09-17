namespace BovineLabs.Core.Editor.Windows.Favourites
{
    using System.Collections.Generic;
    using System.Linq;
    using BovineLabs.Core.Editor.Windows.Base;
    using Unity.Scripting.LifecycleManagement;
    using UnityEditor;
    using Object = UnityEngine.Object;

    public sealed class FavouritesService : BaseObjectService<FavouritesItem, FavouritesPreferences>
    {
        public const string PreferenceKey = "Favourites";

        [NoAutoStaticsCleanup]
        private static FavouritesService instance;

        private readonly List<FavouritesItem> favourites = new();

        private FavouritesService()
            : base(PreferenceKey)
        {
        }

        public override IReadOnlyList<FavouritesItem> Items => this.favourites;

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

        public bool ConfirmRemoval => this.Preferences.ConfirmRemoval;

        public bool AddFavourite(Object obj)
        {
            if (!CanAddFavourite(obj))
            {
                return false;
            }

            var objectId = GlobalObjectId.GetGlobalObjectIdSlow(obj);

            // Check if already exists
            var existingIndex = this.favourites.FindIndex(f => f.MatchesObject(obj, objectId));
            if (existingIndex >= 0)
            {
                return false; // Already exists
            }

            // Add new favourite
            var favouriteItem = new FavouritesItem(obj, objectId);
            this.favourites.Add(favouriteItem);

            this.Save();
            this.NotifyItemsChanged();
            return true;
        }

        public int AddFavourites(IEnumerable<Object> objects)
        {
            var addedCount = 0;

            var assetObjects = objects.Where(CanAddFavourite).ToArray();

            var objectIds = new GlobalObjectId[assetObjects.Length];
            GlobalObjectId.GetGlobalObjectIdsSlow(assetObjects, objectIds);

            for (var index = 0; index < assetObjects.Length; index++)
            {
                var obj = assetObjects[index];
                var objectId = objectIds[index];

                // Check if already exists
                if (this.favourites.Any(f => f.MatchesObject(obj, objectId)))
                {
                    continue;
                }

                // Add new favourite
                var favouriteItem = new FavouritesItem(obj, objectId);
                this.favourites.Add(favouriteItem);
                addedCount++;
            }

            if (addedCount > 0)
            {
                this.Save();
                this.NotifyItemsChanged();
            }

            return addedCount;
        }

        public bool RemoveFavourite(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var objectId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            var index = this.favourites.FindIndex(f => f.MatchesObject(obj, objectId));

            if (index >= 0)
            {
                this.favourites.RemoveAt(index);
                this.Save();
                this.NotifyItemsChanged();
                return true;
            }

            return false;
        }

        public void ClearFavourites()
        {
            if (this.favourites.Count > 0)
            {
                this.favourites.Clear();
                this.Save();
                this.NotifyItemsChanged();
            }
        }

        public bool IsFavourite(Object obj)
        {
            if (obj == null)
            {
                return false;
            }

            var objectId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
            return this.favourites.Any(f => f.MatchesObject(obj, objectId));
        }

        public void ReorderFavourite(int fromIndex, int toIndex)
        {
            if (fromIndex < 0 || fromIndex >= this.favourites.Count ||
                toIndex < 0 || toIndex >= this.favourites.Count ||
                fromIndex == toIndex)
            {
                return;
            }

            var item = this.favourites[fromIndex];
            this.favourites.RemoveAt(fromIndex);
            this.favourites.Insert(toIndex, item);

            this.Save();
            this.NotifyItemsChanged();
        }

        public void SelectFromFavourites(FavouritesItem item)
        {
            this.SelectItem(item);
        }

        internal static bool CanAddFavourite(Object obj)
        {
            return obj != null && AssetDatabase.Contains(obj) && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(obj));
        }

        protected override bool TryRemoveItem(FavouritesItem item)
        {
            return this.favourites.Remove(item);
        }

        protected override void Save()
        {
            this.Preferences.FavouritesData = CreateSerializableItems<FavouritesItem, SerializableFavouriteItem>(this.favourites);
        }

        protected override void Load()
        {
            if (this.Preferences.FavouritesData.Count == 0)
            {
                return;
            }

            var loadedObjects = new LoadedObjectLookup();

            foreach (var item in this.Preferences.FavouritesData)
            {
                if (!LoadedObjectLookup.TryGetTimestamp(item, out var timestamp))
                {
                    continue;
                }

                var obj = loadedObjects.TryGetObject(item, out var savedGlobalId);
                var icon = LoadedObjectLookup.GetIcon(obj);

                var favouriteItem = new FavouritesItem(obj, item.Name, item.TypeName, item.AssetPath, savedGlobalId, icon, timestamp);
                this.favourites.Add(favouriteItem);
            }
        }
    }
}
