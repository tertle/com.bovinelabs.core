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
        private int _maxHistorySize = 10;

        [SerializeField]
        [Tooltip("Whether to track scene objects in selection history")]
        private bool _trackSceneObjects;

        [SerializeField]
        [HideInInspector] // Hide from preferences UI
        private List<SerializableHistoryItem> _lockedHistoryData = new();

        [SerializeField]
        [HideInInspector] // Hide from preferences UI
        private List<SerializableHistoryItem> _normalHistoryData = new();

        public int MaxHistorySize
        {
            get => Math.Max(1, _maxHistorySize);
            set => _maxHistorySize = value;
        }

        public bool TrackSceneObjects
        {
            get => _trackSceneObjects;
            set => _trackSceneObjects = value;
        }

        public List<SerializableHistoryItem> LockedHistoryData
        {
            get => _lockedHistoryData;
            set => _lockedHistoryData = value ?? new List<SerializableHistoryItem>();
        }

        public List<SerializableHistoryItem> NormalHistoryData
        {
            get => _normalHistoryData;
            set => _normalHistoryData = value ?? new List<SerializableHistoryItem>();
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
