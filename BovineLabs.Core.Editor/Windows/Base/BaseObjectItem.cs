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
            Name = name ?? string.Empty;
            TypeName = typeName ?? string.Empty;
            AssetPath = assetPath ?? string.Empty;
            GlobalId = globalObjectId;
            Icon = icon;
            Timestamp = timestamp;

            ObjectRef = obj == null ? new WeakReference(null) : new WeakReference(obj);
            RefreshMetadata();
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
                var obj = ObjectRef.Target as UnityEngine.Object;
                return obj != null && obj.GetType().Name == TypeName;
            }
        }

        public bool IsAsset => !string.IsNullOrEmpty(AssetPath);

        public bool MatchesObject(UnityEngine.Object obj, GlobalObjectId objectId)
        {
            if (obj == null)
            {
                return false;
            }

            return ObjectRef.Target is UnityEngine.Object current && current != null && current == obj ||
                HasValidObjectId(GlobalId) && HasValidObjectId(objectId) && GlobalId.Equals(objectId);
        }

        public void RefreshMetadata()
        {
            var obj = ObjectRef.Target as UnityEngine.Object;
            if (obj == null || obj.GetType().Name != TypeName)
            {
                return;
            }

            Name = obj.name;
            AssetPath = AssetDatabase.GetAssetPath(obj);
        }

        public UnityEngine.Object GetObject()
        {
            // First try to get from weak reference (fastest)
            var obj = ObjectRef.Target as UnityEngine.Object;

            // Unity replaces assets with the importer (MonoImporter, AssetImporter) when unloading an asset so it appears loaded, but it's the wrong type
            if (obj != null && obj.GetType().Name == TypeName)
            {
                RefreshMetadata();
                return obj;
            }

            // If weak reference is null, try to reload using GlobalObjectId
            if (HasValidObjectId(GlobalId))
            {
                obj = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(GlobalId);
                if (obj != null && (obj is not AssetImporter || obj.GetType().Name == TypeName))
                {
                    // Update the weak reference for future calls
                    ObjectRef.Target = obj;
                    TypeName = obj.GetType().Name;
                    RefreshMetadata();
                    Icon = AssetPreview.GetMiniThumbnail(obj);
                    return obj;
                }
            }

            // Old preference entries may not have a valid GlobalObjectId. Path fallback is only safe when no exact identity was persisted.
            if (!HasValidObjectId(GlobalId) && !string.IsNullOrEmpty(AssetPath))
            {
                obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(AssetPath);
                if (obj != null && obj.GetType().Name == TypeName)
                {
                    ObjectRef.Target = obj;
                    GlobalId = GlobalObjectId.GetGlobalObjectIdSlow(obj);
                    RefreshMetadata();
                    Icon = AssetPreview.GetMiniThumbnail(obj);
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
            if (IsAlive)
            {
                GlobalId = GlobalObjectId.GetGlobalObjectIdSlow((UnityEngine.Object)ObjectRef.Target);
            }
        }

        public string GetDisplayText(bool showTimestamps = true, bool showAssetPaths = true, bool showTypeNames = true, string timestampFormat = "HH:mm:ss")
        {
            var result = Name;

            // Add type information if enabled
            if (showTypeNames)
            {
                result += $" ({TypeName})";
            }

            // Add timestamp if enabled
            if (showTimestamps)
            {
                var timeStr = Timestamp.ToString(timestampFormat);
                result = $"[{timeStr}] {result}";
            }

            // Add path information if enabled
            if (showAssetPaths)
            {
                var pathStr = IsAsset ? $" [{AssetPath}]" : " (Scene)";
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
