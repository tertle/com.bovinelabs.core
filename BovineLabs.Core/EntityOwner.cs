namespace BovineLabs.Core
{
    using Unity.Entities;

    /// <summary>The primary gameplay entity that owns this additional entity.</summary>
    public struct EntityOwner : IComponentData
    {
        public Entity Value;
    }
}
