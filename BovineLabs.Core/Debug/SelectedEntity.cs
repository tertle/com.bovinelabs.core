namespace BovineLabs.Core
{
    using Unity.Entities;

    public struct SelectedEntity : IComponentData
    {
        public Entity Value;
    }
}
