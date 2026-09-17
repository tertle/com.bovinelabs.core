namespace BovineLabs.Core.Utility
{
    using Unity.Entities;

    [UpdateInGroup(typeof(InitializationSystemGroup), OrderFirst = true)]
    public abstract partial class InitSystemBase : SystemBase
    {
        protected override void OnUpdate()
        {
            var initialization = this.World.GetExistingSystemManaged<InitializationSystemGroup>();
            initialization.RemoveSystemFromUpdateList(this);
        }
    }
}
