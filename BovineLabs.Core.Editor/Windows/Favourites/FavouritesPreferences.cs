namespace BovineLabs.Core.Editor.Windows.Favourites
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.EditorPreferences;
    using BovineLabs.Core.Editor.Windows.Base;
    using UnityEngine;

    [CoreEditorPreference("Favourites")]
    [Serializable]
    public class FavouritesPreferences : BaseDisplayPreferences
    {
        [SerializeField]
        [Tooltip("Whether to show confirmation dialog when removing favourites")]
        private bool confirmRemoval = true;

        [SerializeField]
        [HideInInspector] // Hide from preferences UI
        private List<SerializableFavouriteItem> favouritesData = new();

        public FavouritesPreferences()
        {
            // Set defaults specific to favourites
            this.GreyOutUnloadedObjects = true;
        }

        public bool ConfirmRemoval
        {
            get => this.confirmRemoval;
            set => this.confirmRemoval = value;
        }

        public List<SerializableFavouriteItem> FavouritesData
        {
            get => this.favouritesData;
            set => this.favouritesData = value ?? new List<SerializableFavouriteItem>();
        }

        public override string[] GetSearchKeywords()
        {
            return IEditorPreference.GetSearchKeywordsFromType(typeof(FavouritesPreferences));
        }
    }

    [Serializable]
    public class SerializableFavouriteItem : SerializableObjectItem
    {
    }
}
