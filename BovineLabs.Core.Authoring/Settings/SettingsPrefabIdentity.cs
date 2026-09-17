namespace BovineLabs.Core.Authoring.Settings
{
    using Unity.Entities;

    internal struct SettingsPrefabIdentity : IComponentData
    {
        public Hash128 PrefabGuid;
    }
}
