// <copyright file="SelectionHistoryItem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.SelectionHistory
{
    using System;
    using BovineLabs.Core.Editor.Windows.Base;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Represents a single item in the selection history.
    /// </summary>
    public sealed class SelectionHistoryItem : BaseObjectItem
    {
        public SelectionHistoryItem(UnityEngine.Object obj, GlobalObjectId objectId, bool isLocked)
            : base(obj, objectId)
        {
            this.IsLocked = isLocked;
        }

        public SelectionHistoryItem(
            UnityEngine.Object? obj, string name, string typeName, string assetPath, GlobalObjectId globalObjectId, Texture2D? icon, DateTime timestamp,
            bool isLocked)
            : base(obj, name, typeName, assetPath, globalObjectId, icon, timestamp)
        {
            this.IsLocked = isLocked;
        }

        /// <summary>Gets or sets a value indicating whether this item is locked to the top of the list.</summary>
        public bool IsLocked { get; set; }
    }
}
