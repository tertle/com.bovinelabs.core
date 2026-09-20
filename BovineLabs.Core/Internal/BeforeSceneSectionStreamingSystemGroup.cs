namespace BovineLabs.Core.Internal
{
    using Unity.Entities;
    using Unity.Scenes;

    /// <summary>
    /// Provides a hook after scene references resolve but before scene sections start streaming.
    /// </summary>
    [UpdateBefore(typeof(SceneSectionStreamingSystem))]
    [UpdateAfter(typeof(ResolveSceneReferenceSystem))]
    [UpdateInGroup(typeof(SceneSystemGroup))]
    public partial class BeforeSceneSectionStreamingSystemGroup : ComponentSystemGroup
    {
    }
}
