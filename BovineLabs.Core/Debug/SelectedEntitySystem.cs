#if !UNITY_EDITOR && UNITY_INCLUDE_INSTRUMENTATION
namespace BovineLabs.Core
{
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializationSystemGroup))]
    public partial class SelectedEntitySystem : SystemBase
    {
        protected override void OnCreate()
        {
            EntityManager.CreateEntity(typeof(SelectedEntity), typeof(SelectedEntities));
        }

        protected override void OnUpdate()
        {
            // In debug builds you can't select entities, at least not via this system
            World.GetExistingSystemManaged<InitializationSystemGroup>().RemoveSystemFromUpdateList(this);
        }
    }
}
#endif
