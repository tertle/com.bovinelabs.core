namespace BovineLabs.Core.Extensions
{
    using Unity.Entities;
    using Unity.Entities.Serialization;

    public static class EntitySceneReferenceExtensions
    {
        public static Hash128 SceneGUID(this EntitySceneReference entitySceneReference)
        {
            return entitySceneReference.Id.GlobalId.AssetGUID;
        }
    }
}
