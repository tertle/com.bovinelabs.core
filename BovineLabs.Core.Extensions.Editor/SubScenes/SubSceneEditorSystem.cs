// <copyright file="SubSceneEditorSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using BovineLabs.Core.Authoring.SubScenes;
    using BovineLabs.Core.ConfigVars;
    using BovineLabs.Core.Editor.Settings;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Core.Groups;
    using BovineLabs.Core.SubScenes;
    using Unity.Burst;
    using Unity.Entities;

    [Configurable]
    [UpdateInGroup(typeof(AfterSceneSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(Worlds.SimulationThinService)]
    [CreateAfter(typeof(BLDebugSystem))]
    [UpdateBefore(typeof(SubSceneLoadingSystem))]
    public partial class SubSceneEditorSystem : SystemBase
    {
        [ConfigVar("debug.subscene-override", -1, "", true, true)]
        public static readonly SharedStatic<int> Override = SharedStatic<int>.GetOrCreate<SubSceneEditorSystem>();

        private SubSceneEditorSet? set;

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            var sets = EditorSettingsUtility.GetSettings<SubSceneSettings>().EditorSceneSets;
            var index = Override.Data;

            this.RequireForUpdate<RequireForLoading>();

            if (index < 0 || index >= sets.Count || !sets[index])
            {
                this.Enabled = false;
                return;
            }

            this.set = sets[index];

            this.EntityManager.CreateSingleton<RequireForLoading>();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            var query = SystemAPI.QueryBuilder().WithAll<SubSceneLoadData>().WithAllRW<LoadSubScene>().Build();

            // The default loading script will always exist so wait for that to load something
            if (query.CalculateEntityCountWithoutFiltering() <= 1)
            {
                return;
            }

            // Stop existing non-required subscenes from loading
            foreach (var (s, e) in SystemAPI.Query<RefRO<SubSceneLoadData>, EnabledRefRW<LoadSubScene>>())
            {
                if (s.ValueRO.IsRequired)
                {
                    continue;
                }

                e.ValueRW = false;
            }

            var commands = new EntityManagerCommands(this.EntityManager);
            commands.CreateEntity();
            SubSceneAuthUtil.AddComponents(ref commands, -2, this.set!.TargetWorld, true, true, true, this.set.Scenes);

            this.Enabled = false;
            this.EntityManager.DestroyEntity(SystemAPI.GetSingletonEntity<RequireForLoading>());
        }

        // Component to force this system never to run again in case it's accidently re-enabled
        private struct RequireForLoading : IComponentData
        {
        }
    }
}
#endif
