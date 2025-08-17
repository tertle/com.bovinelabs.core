// <copyright file="BaseDisplayPreferences.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.Base
{
    using System;
    using BovineLabs.Core.Editor.EditorPreferences;
    using Unity.Properties;
    using UnityEngine;

    /// <summary>
    /// Base class for display preferences shared across object list windows.
    /// </summary>
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

        /// <summary>
        /// Event fired when any preference value changes.
        /// </summary>
        public event Action? PreferencesChanged;

        /// <summary>
        /// Gets or sets the item height.
        /// </summary>
        public int ItemHeight
        {
            get => Math.Clamp(this.itemHeight, 16, 64);
            set => this.itemHeight = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use a monospace font for the list.
        /// </summary>
        public bool UseMonospaceFont
        {
            get => this.useMonospaceFont;
            set => this.useMonospaceFont = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show icons.
        /// </summary>
        public bool ShowIcons
        {
            get => this.showIcons;
            set => this.showIcons = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show timestamps.
        /// </summary>
        public bool ShowTimestamps
        {
            get => this.showTimestamps;
            set => this.showTimestamps = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show asset paths.
        /// </summary>
        public bool ShowAssetPaths
        {
            get => this.showAssetPaths;
            set => this.showAssetPaths = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show type names in the list.
        /// </summary>
        public bool ShowTypeNames
        {
            get => this.showTypeNames;
            set => this.showTypeNames = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to show the status bar.
        /// </summary>
        public bool ShowStatusBar
        {
            get => this.showStatusBar;
            set => this.showStatusBar = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to grey out unloaded objects.
        /// </summary>
        public bool GreyOutUnloadedObjects
        {
            get => this.greyOutUnloadedObjects;
            set => this.greyOutUnloadedObjects = value;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to highlight currently selected objects in the list.
        /// </summary>
        public bool HighlightCurrentSelection
        {
            get => this.highlightCurrentSelection;
            set => this.highlightCurrentSelection = value;
        }

        /// <summary>
        /// Gets or sets the double-click threshold in seconds.
        /// </summary>
        public float DoubleClickThreshold
        {
            get => Math.Clamp(this.doubleClickThreshold, 0.1f, 1.0f);
            set => this.doubleClickThreshold = value;
        }

        /// <inheritdoc />
        public void OnPreferenceChanged(PropertyPath path)
        {
            // Preferences are automatically saved by UserSettings system
            // Notify any listeners that preferences have changed
            this.PreferencesChanged?.Invoke();
        }

        /// <inheritdoc />
        public abstract string[] GetSearchKeywords();
    }
}