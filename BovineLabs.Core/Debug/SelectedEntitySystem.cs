#if !UNITY_EDITOR && UNITY_INCLUDE_INSTRUMENTATION
namespace BovineLabs.Core
{
    using Unity.Entities;

    /// <summary>
    /// This system does nothing except create the <see cref="SelectedEntity"/> in debug builds.
    /// In editor this is handled by SelectedEntityEditorSystem.
    /// </summary>
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SelectedEntitySystem : SystemBase
    {
        protected override void OnCreate()
        {
            this.EntityManager.CreateEntity(typeof(SelectedEntity), typeof(SelectedEntities));
        }

        protected override void OnUpdate()
        {
            // In debug builds you can't select entities, at least not via this system
            this.World.GetExistingSystemManaged<InitializationSystemGroup>().RemoveSystemFromUpdateList(this);
        }
    }
}
#endif
