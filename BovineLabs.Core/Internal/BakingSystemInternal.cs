namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static class BakingSystemInternal
    {
        public static Hash128 SceneGUID(this BakingSystem bakingSystem)
        {
            return bakingSystem.BakingSettings.SceneGUID;
        }
    }
}
