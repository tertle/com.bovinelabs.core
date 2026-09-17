namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using Unity.Entities.Build;

    public static class BakingSystemInternals
    {
        public static IEntitiesPlayerSettings GetPlayerSettings(this BakingSystem bakingSystem)
        {
            return bakingSystem.BakingSettings.DotsSettings;
        }
    }
}
