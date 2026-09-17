namespace BovineLabs.Core.Editor.Windows.Base
{
    using System;
    using UnityEditor;
    using UnityEngine;

    public abstract class BaseObjectItem
    {
        protected BaseObjectItem(UnityEngine.Object obj, GlobalObjectId objectId)
            : this(obj, obj.name, obj.GetType().Name, AssetDatabase.GetAssetPath(obj), objectId, AssetPreview.GetMiniThumbnail(obj), DateTime.Now)
        {
        }

        protected BaseObjectItem(
            UnityEngine.Object obj, string name, string typeName, string assetPath, GlobalObjectId globalObjectId, Texture2D icon, DateTime timestamp)
        {
            this.Name = name ?? string.Empty;
            this.TypeName = typeName ?? string.Empty;
            this.AssetPath = assetPath ?? string.Empty;
            this.GlobalId = globalObjectId;
            this.Icon = icon;
            this.Timestamp = timestamp;

            this.ObjectRef = obj == null ? new WeakReference(null) : new WeakReference(obj);
            this.RefreshMetadata();
        }

        public string Name { get; private set; }

        public string TypeName { get; private set; }

        public string AssetPath { get; set; }

        public DateTime Timestamp { get; }

        public WeakReference ObjectRef { get; }

        public Texture2D Icon { get; private set; }

        public GlobalObjectId GlobalId { get; private set; }

        public bool IsAlive
        {
            get
            {
                var obj = this.ObjectRef.Target as UnityEngine.Object;
                return obj != null && obj.GetType().Name == this.TypeName;
            }
        }

        public bool IsAsset => !string.IsNullOrEmpty(this.AssetPath);

        public bool MatchesObject(UnityEngine.Object obj, GlobalObjectId objectId)
        {
            if (obj == null)
            {
                return false;
            }

            return this.ObjectRef.Target is UnityEngine.Object current && current != null && current == obj ||
                HasValidObjectId(this.GlobalId) && HasValidObjectId(objectId) && this.GlobalId.Equals(objectId);
        }

        public void RefreshMetadata()
        {
            var obj = this.ObjectRef.Target as UnityEngine.Object;
            if (obj == null || obj.GetType().Name != this.TypeName)
            {
                return;
            }

            this.Name = obj.name;
            this.AssetPath = AssetDatabase.GetAssetPath(obj);
        }

        public UnityEngine.Object GetObject()
        {
            // First try to get from weak reference (fastest)
            var obj = this.ObjectRef.Target as UnityEngine.Object;

            // Unity replaces assets with the importer (MonoImporter, AssetImporter) when unloading an asset so it appears loaded, but it's the wrong type
            if (obj != null && obj.GetType().Name == this.TypeName)
            {
                this.RefreshMetadata();
                return obj;
            }

            // If weak reference is null, try to reload using GlobalObjectId
            if (HasValidObjectId(this.GlobalId))
            {
                obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(this.GlobalId);
                if (obj != null && (obj is not AssetImporter || obj.GetType().Name == this.TypeName))
                {
                    // Update the weak reference for future calls
                    this.ObjectRef.Target = obj;
                    this.TypeName = obj.GetType().Name;
                    this.RefreshMetadata();
                    this.Icon = AssetPreview.GetMiniThumbnail(obj);
                    return obj;
                }
            }

            // Old preference entries may not have a valid GlobalObjectId. Path fallback is only safe when no exact identity was persisted.
            if (!HasValidObjectId(this.GlobalId) && !string.IsNullOrEmpty(this.AssetPath))
            {
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(this.AssetPath);
                if (obj != null && obj.GetType().Name == this.TypeName)
                {
                    this.ObjectRef.Target = obj;
                    this.GlobalId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                    this.RefreshMetadata();
                    this.Icon = AssetPreview.GetMiniThumbnail(obj);
                    return obj;
                }
            }

            return null;
        }

        internal static bool HasValidObjectId(GlobalObjectId objectId)
        {
            return !objectId.assetGUID.Empty() && objectId.identifierType != 0;
        }

        internal void RefreshIdentity()
        {
            if (this.IsAlive)
            {
                this.GlobalId = GlobalObjectId.GetGlobalObjectIdSlow((UnityEngine.Object)this.ObjectRef.Target);
            }
        }

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

    [Serializable]
    public abstract class SerializableObjectItem
    {
        public string Name = string.Empty;
        public string TypeName = string.Empty;
        public string AssetPath = string.Empty;
        public long Timestamp;
        public string GlobalIdString = string.Empty;
        public string Icon = string.Empty;
    }
}
