namespace BovineLabs.Core.Editor.Windows.SelectionHistory
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.EditorPreferences;
    using BovineLabs.Core.Editor.Windows.Base;
    using UnityEngine;

    [CoreEditorPreference("Selection History")]
    [Serializable]
    public class SelectionHistoryPreferences : BaseDisplayPreferences
    {
        [SerializeField]
        [Tooltip("Maximum number of unlocked items to keep in selection history")]
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

        public int MaxHistorySize
        {
            get => Math.Max(1, this.maxHistorySize);
            set => this.maxHistorySize = value;
        }

        public bool TrackSceneObjects
        {
            get => this.trackSceneObjects;
            set => this.trackSceneObjects = value;
        }

        public List<SerializableHistoryItem> LockedHistoryData
        {
            get => this.lockedHistoryData;
            set => this.lockedHistoryData = value ?? new List<SerializableHistoryItem>();
        }

        public List<SerializableHistoryItem> NormalHistoryData
        {
            get => this.normalHistoryData;
            set => this.normalHistoryData = value ?? new List<SerializableHistoryItem>();
        }

        public override string[] GetSearchKeywords()
        {
            return IEditorPreference.GetSearchKeywordsFromType(typeof(SelectionHistoryPreferences));
        }
    }

    [Serializable]
    public class SerializableHistoryItem : SerializableObjectItem
    {
        public bool IsLocked;
    }
}
