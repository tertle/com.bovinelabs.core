namespace BovineLabs.Core
{
    using Unity.Entities;
    using WorldFlag = Unity.Entities.WorldSystemFilterFlags;

    /// <summary>
    /// Groups diagnostic systems in presentation across supported simulation and editor worlds.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    [WorldSystemFilter(WorldFlag.LocalSimulation | WorldFlag.ClientSimulation | WorldFlag.ServerSimulation | WorldFlag.ThinClientSimulation | WorldFlag.Editor,
        WorldFlag.LocalSimulation | WorldFlag.ClientSimulation | WorldFlag.ServerSimulation)]
    public partial class DebugSystemGroup : ComponentSystemGroup
    {
    }
}
