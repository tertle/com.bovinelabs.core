namespace BovineLabs.Core.Editor.Settings
{
    using Unity.Entities;
    using UnityEditor;

    internal struct EditorSettingsFallbackState
    {
        public Hash128 PrefabGuid;
        public Entity SceneEntity;
        public Entity Root;
        public Entity Instance;
        public int AuthoritativeCount;

        public readonly string Path => AssetDatabase.GUIDToAssetPath(PrefabGuid.ToString());
    }
}
