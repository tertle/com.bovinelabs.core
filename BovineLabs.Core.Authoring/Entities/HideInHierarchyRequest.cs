namespace BovineLabs.Core.Authoring.Entities
{
    using Unity.Entities;

    /// <summary>Asks <see cref="HideInHierarchyBakingSystem"/> to hide this entity and its linked entity group members.</summary>
    [BakingType]
    internal struct HideInHierarchyRequest : IComponentData
    {
    }
}
