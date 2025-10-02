// <copyright file="BaseObjectItem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Windows.Base
{
    using System;
    using UnityEditor;
    using UnityEngine;

    /// <summary>
    /// Base class for object items that can be displayed in editor windows.
    /// Provides common functionality for managing Unity object references.
    /// </summary>
    public abstract class BaseObjectItem
    {
        protected BaseObjectItem(UnityEngine.Object obj, GlobalObjectId objectId)
            : this(obj, obj.name, obj.GetType().Name, AssetDatabase.GetAssetPath(obj), objectId, AssetPreview.GetMiniThumbnail(obj), DateTime.Now)
        {
        }

        protected BaseObjectItem(
            UnityEngine.Object? obj, string name, string typeName, string assetPath, GlobalObjectId globalObjectId, Texture2D? icon, DateTime timestamp)
        {
            this.Name = name;
            this.TypeName = typeName;
            this.AssetPath = assetPath;
            this.GlobalId = globalObjectId;
            this.Icon = icon;
            this.Timestamp = timestamp;

            this.ObjectRef = obj == null ? new WeakReference(null) : new WeakReference(obj);
        }

        /// <summary>Gets the display name of the object.</summary>
        public string Name { get; }

        /// <summary>Gets the type name of the object.</summary>
        public string TypeName { get; }

        /// <summary>Gets or sets the asset path if this is an asset, empty otherwise.</summary>
        public string AssetPath { get; set; }

        /// <summary>Gets the timestamp when this object was added.</summary>
        public DateTime Timestamp { get; }

        /// <summary>Gets the weak reference to the original object.</summary>
        public WeakReference ObjectRef { get; }

        /// <summary>Gets the icon/thumbnail for the object.</summary>
        public Texture2D? Icon { get; private set; }

        /// <summary>Gets the GlobalObjectId for persistent object identification.</summary>
        public GlobalObjectId GlobalId { get; }

        /// <summary>Gets a value indicating whether the referenced object is still alive.</summary>
        public bool IsAlive => this.ObjectRef is { IsAlive: true, Target: UnityEngine.Object } && this.ObjectRef.Target.GetType().Name == this.TypeName;

        /// <summary>Gets a value indicating whether this is an asset (vs scene object).</summary>
        public bool IsAsset => !string.IsNullOrEmpty(this.AssetPath);

        /// <summary>Gets the referenced object if it's still alive.</summary>
        /// <returns>The object.</returns>
        public UnityEngine.Object? GetObject()
        {
            // First try to get from weak reference (fastest)
            var obj = this.ObjectRef.Target as UnityEngine.Object;

            // Unity replaces assets with the importer (MonoImporter, AssetImporter) when unloading an asset so it appears loaded, but it's the wrong type
            if (obj != null && obj.GetType().Name == this.TypeName)
            {
                return obj;
            }

            // If weak reference is null, try to reload using GlobalObjectId
            if (!this.GlobalId.assetGUID.Empty() || this.GlobalId.identifierType != 0)
            {
                obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(this.GlobalId);
                if (obj != null)
                {
                    // Update the weak reference for future calls
                    this.ObjectRef.Target = obj;
                    this.AssetPath = AssetDatabase.GetAssetPath(obj);
                    this.Icon = AssetPreview.GetMiniThumbnail(obj);
                    return obj;
                }
            }

            // Finally, try asset path for assets (fallback)
            if (!string.IsNullOrEmpty(this.AssetPath))
            {
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(this.AssetPath);
                if (obj != null)
                {
                    this.ObjectRef.Target = obj;
                    this.Icon = AssetPreview.GetMiniThumbnail(obj);
                    return obj;
                }
            }

            return null;
        }

        /// <summary>Gets a display string for the item.</summary>
        /// <param name="showTimestamps">Should timestamps be shown.</param>
        /// <param name="showAssetPaths">Should the asset path be shown.</param>
        /// <param name="showTypeNames">Should type names be shown.</param>
        /// <param name="timestampFormat">Format string for timestamp display.</param>
        /// <returns>The display text.</returns>
        public string GetDisplayText(bool showTimestamps = true, bool showAssetPaths = true, bool showTypeNames = true, string timestampFormat = "HH:mm:ss")
        {
            var result = this.Name;

            // Add type information if enabled
            if (showTypeNames)
            {
                result += $" ({this.TypeName})";
            }

            // Add timestamp if enabled
            if (showTimestamps)
            {
                var timeStr = this.Timestamp.ToString(timestampFormat);
                result = $"[{timeStr}] {result}";
            }

            // Add path information if enabled
            if (showAssetPaths)
            {
                var pathStr = this.IsAsset ? $" [{this.AssetPath}]" : " (Scene)";
                result += pathStr;
            }

            return result;
        }
    }
}