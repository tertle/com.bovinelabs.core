// <copyright file="SelectionHistoryPreferences.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.SelectionHistory
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.EditorPreferences;
    using BovineLabs.Core.Editor.Windows.Base;
    using UnityEngine;

    /// <summary>
    /// Editor preferences for Selection History feature.
    /// </summary>
    [CoreEditorPreference("Selection History")]
    [Serializable]
    public class SelectionHistoryPreferences : BaseDisplayPreferences
    {
        [SerializeField]
        [Tooltip("Maximum number of items to keep in selection history")]
        [Min(1)]
        private int maxHistorySize = 10;

        [SerializeField]
        [Tooltip("Whether to track scene objects in selection history")]
        private bool trackSceneObjects;

        [SerializeField]
        [HideInInspector] // Hide from preferences UI
        private List<SerializableHistoryItem> lockedHistoryData = new();

        [SerializeField]
        [HideInInspector] // Hide from preferences UI
        private List<SerializableHistoryItem> normalHistoryData = new();

        /// <summary>
        /// Gets or sets the maximum number of items to keep in selection history.
        /// </summary>
        public int MaxHistorySize
        {
            get => Math.Max(10, this.maxHistorySize);
            set => this.maxHistorySize = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to track scene objects in selection history.
        /// </summary>
        public bool TrackSceneObjects
        {
            get => this.trackSceneObjects;
            set => this.trackSceneObjects = value;
        }

        /// <summary>
        /// Gets or sets the locked history data. Hidden from preferences UI.
        /// </summary>
        public List<SerializableHistoryItem> LockedHistoryData
        {
            get => this.lockedHistoryData;
            set => this.lockedHistoryData = value ?? new List<SerializableHistoryItem>();
        }

        /// <summary>
        /// Gets or sets the normal history data. Hidden from preferences UI.
        /// </summary>
        public List<SerializableHistoryItem> NormalHistoryData
        {
            get => this.normalHistoryData;
            set => this.normalHistoryData = value ?? new List<SerializableHistoryItem>();
        }

        /// <inheritdoc />
        public override string[] GetSearchKeywords()
        {
            return IEditorPreference.GetSearchKeywordsFromType(typeof(SelectionHistoryPreferences));
        }
    }

    [Serializable]
    public class SerializableHistoryItem
    {
        public string Name = string.Empty;
        public string TypeName = string.Empty;
        public string AssetPath = string.Empty;
        public long Timestamp;
        public bool IsLocked;
        public string GlobalIdString = string.Empty;
        public string Icon = string.Empty;
    }
}
