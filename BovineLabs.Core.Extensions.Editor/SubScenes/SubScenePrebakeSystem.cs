// <copyright file="SubScenePrebakeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using System.Collections.Generic;
    using BovineLabs.Core.Editor.Settings;
    using Unity.Entities;
    using Unity.Scenes;
    using UnityEditor;
    using UnityEngine;
    using EditorSettings = BovineLabs.Core.Editor.Settings.EditorSettings;

    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    [CreateAfter(typeof(SceneSectionStreamingSystem))]
    [UpdateInGroup(typeof(SceneSystemGroup))]
    public partial class SubScenePrebakeSystem : SystemBase
    {
        private readonly List<GUID> guids = new();

        /// <inheritdoc />
        protected override void OnCreate()
        {
            if (!EditorSettingsUtility.TryGetSettings<EditorSettings>(out var settings))
            {
                return;
            }

            foreach (var scene in settings!.PrebakeScenes)
            {
                if (!scene)
                {
                    continue;
                }

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scene, out var guid, out _))
                {
                    continue;
                }

                var g = new GUID(guid);
                this.guids.Add(g);
                SceneSystem.LoadSceneAsync(this.World.Unmanaged, g);
            }
        }

        // OnDestroy fails due to other system being destroyed
        protected override void OnStopRunning()
        {
            foreach (var guid in this.guids)
            {
                SceneSystem.UnloadScene(this.World.Unmanaged, guid, SceneSystem.UnloadParameters.DestroyMetaEntities);
            }

            this.guids.Clear();
        }

        protected override void OnUpdate()
        {
        }
    }
}
#endif
