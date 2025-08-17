// <copyright file="FavouritesPreferences.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.Favourites
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.EditorPreferences;
    using BovineLabs.Core.Editor.Windows.Base;
    using UnityEngine;

    /// <summary>
    /// Editor preferences for Favourites feature.
    /// </summary>
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

        /// <summary>
        /// Gets or sets a value indicating whether to show confirmation dialog when removing favourites.
        /// </summary>
        public bool ConfirmRemoval
        {
            get => this.confirmRemoval;
            set => this.confirmRemoval = value;
        }

        /// <summary>
        /// Gets or sets the favourites data. Hidden from preferences UI.
        /// </summary>
        public List<SerializableFavouriteItem> FavouritesData
        {
            get => this.favouritesData;
            set => this.favouritesData = value ?? new List<SerializableFavouriteItem>();
        }

        /// <inheritdoc />
        public override string[] GetSearchKeywords()
        {
            return IEditorPreference.GetSearchKeywordsFromType(typeof(FavouritesPreferences));
        }
    }

    [Serializable]
    public class SerializableFavouriteItem
    {
        public string Name = string.Empty;
        public string TypeName = string.Empty;
        public string AssetPath = string.Empty;
        public long Timestamp;
        public string GlobalIdString = string.Empty;
        public string Icon = string.Empty;
    }
}