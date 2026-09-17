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
        private int itemHeight = 27;

        [SerializeField]
        [Tooltip("Whether to use a monospace font for the list")]
        private bool useMonospaceFont;

        [SerializeField]
        [Tooltip("Whether to show icons/thumbnails for objects")]
        private bool showIcons = true;

        [SerializeField]
        [Tooltip("Whether to show timestamps")]
        private bool showTimestamps;

        [SerializeField]
        [Tooltip("Whether to show asset paths for assets")]
        private bool showAssetPaths;

        [SerializeField]
        [Tooltip("Whether to show type names (e.g. GameObject) in the list")]
        private bool showTypeNames = true;

        [SerializeField]
        [Tooltip("Whether to show the status bar at the bottom of the window")]
        private bool showStatusBar;

        [SerializeField]
        [Tooltip("Whether to grey out objects that no longer exist or are missing")]
        private bool greyOutUnloadedObjects;

        [SerializeField]
        [Tooltip("Whether to highlight currently selected objects in the list")]
        private bool highlightCurrentSelection = true;

        [SerializeField]
        [Range(0.1f, 1.0f)]
        [Tooltip("Time threshold for double-click detection in seconds")]
        private float doubleClickThreshold = 0.3f;

        public event Action PreferencesChanged;

        public int ItemHeight
        {
            get => Math.Clamp(this.itemHeight, 16, 64);
            set => this.itemHeight = value;
        }

        public bool UseMonospaceFont
        {
            get => this.useMonospaceFont;
            set => this.useMonospaceFont = value;
        }

        public bool ShowIcons
        {
            get => this.showIcons;
            set => this.showIcons = value;
        }

        public bool ShowTimestamps
        {
            get => this.showTimestamps;
            set => this.showTimestamps = value;
        }

        public bool ShowAssetPaths
        {
            get => this.showAssetPaths;
            set => this.showAssetPaths = value;
        }

        public bool ShowTypeNames
        {
            get => this.showTypeNames;
            set => this.showTypeNames = value;
        }

        public bool ShowStatusBar
        {
            get => this.showStatusBar;
            set => this.showStatusBar = value;
        }

        public bool GreyOutUnloadedObjects
        {
            get => this.greyOutUnloadedObjects;
            set => this.greyOutUnloadedObjects = value;
        }

        public bool HighlightCurrentSelection
        {
            get => this.highlightCurrentSelection;
            set => this.highlightCurrentSelection = value;
        }

        /// <summary>
        /// Threshold in seconds.
        /// </summary>
        public float DoubleClickThreshold
        {
            get => Math.Clamp(this.doubleClickThreshold, 0.1f, 1.0f);
            set => this.doubleClickThreshold = value;
        }

        public void OnPreferenceChanged(PropertyPath path)
        {
            // Preferences are automatically saved by UserSettings system
            // Notify any listeners that preferences have changed
            this.PreferencesChanged?.Invoke();
        }

        public abstract string[] GetSearchKeywords();
    }
}