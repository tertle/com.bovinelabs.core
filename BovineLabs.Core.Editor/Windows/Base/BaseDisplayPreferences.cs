namespace BovineLabs.Core.Editor.Windows.Base
{
    using System;
    using BovineLabs.Core.Editor.EditorPreferences;
    using Unity.Properties;
    using UnityEngine;

    [Serializable]
    public abstract class BaseDisplayPreferences : IEditorPreference
    {
        [SerializeField]
        [Range(20, 40)]
        [Tooltip("Height of each item in pixels")]
        private int _itemHeight = 27;

        [SerializeField]
        [Tooltip("Whether to use a monospace font for the list")]
        private bool _useMonospaceFont;

        [SerializeField]
        [Tooltip("Whether to show icons/thumbnails for objects")]
        private bool _showIcons = true;

        [SerializeField]
        [Tooltip("Whether to show timestamps")]
        private bool _showTimestamps;

        [SerializeField]
        [Tooltip("Whether to show asset paths for assets")]
        private bool _showAssetPaths;

        [SerializeField]
        [Tooltip("Whether to show type names (e.g. GameObject) in the list")]
        private bool _showTypeNames = true;

        [SerializeField]
        [Tooltip("Whether to show the status bar at the bottom of the window")]
        private bool _showStatusBar;

        [SerializeField]
        [Tooltip("Whether to grey out objects that no longer exist or are missing")]
        private bool _greyOutUnloadedObjects;

        [SerializeField]
        [Tooltip("Whether to highlight currently selected objects in the list")]
        private bool _highlightCurrentSelection = true;

        [SerializeField]
        [Range(0.1f, 1.0f)]
        [Tooltip("Time threshold for double-click detection in seconds")]
        private float _doubleClickThreshold = 0.3f;

        public event Action PreferencesChanged;

        public int ItemHeight
        {
            get => Math.Clamp(_itemHeight, 16, 64);
            set => _itemHeight = value;
        }

        public bool UseMonospaceFont
        {
            get => _useMonospaceFont;
            set => _useMonospaceFont = value;
        }

        public bool ShowIcons
        {
            get => _showIcons;
            set => _showIcons = value;
        }

        public bool ShowTimestamps
        {
            get => _showTimestamps;
            set => _showTimestamps = value;
        }

        public bool ShowAssetPaths
        {
            get => _showAssetPaths;
            set => _showAssetPaths = value;
        }

        public bool ShowTypeNames
        {
            get => _showTypeNames;
            set => _showTypeNames = value;
        }

        public bool ShowStatusBar
        {
            get => _showStatusBar;
            set => _showStatusBar = value;
        }

        public bool GreyOutUnloadedObjects
        {
            get => _greyOutUnloadedObjects;
            set => _greyOutUnloadedObjects = value;
        }

        public bool HighlightCurrentSelection
        {
            get => _highlightCurrentSelection;
            set => _highlightCurrentSelection = value;
        }

        /// <summary>
        /// Threshold in seconds.
        /// </summary>
        public float DoubleClickThreshold
        {
            get => Math.Clamp(_doubleClickThreshold, 0.1f, 1.0f);
            set => _doubleClickThreshold = value;
        }

        public void OnPreferenceChanged(PropertyPath path)
        {
            // Preferences are automatically saved by UserSettings system
            // Notify any listeners that preferences have changed
            PreferencesChanged?.Invoke();
        }

        public abstract string[] GetSearchKeywords();
    }
}