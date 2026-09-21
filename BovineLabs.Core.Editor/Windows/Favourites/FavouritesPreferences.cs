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
        private bool _confirmRemoval = true;

        [SerializeField]
        [HideInInspector] // Hide from preferences UI
        private List<SerializableFavouriteItem> _favouritesData = new();

        public FavouritesPreferences()
        {
            // Set defaults specific to favourites
            GreyOutUnloadedObjects = true;
        }

        public bool ConfirmRemoval
        {
            get => _confirmRemoval;
            set => _confirmRemoval = value;
        }

        public List<SerializableFavouriteItem> FavouritesData
        {
            get => _favouritesData;
            set => _favouritesData = value ?? new List<SerializableFavouriteItem>();
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
