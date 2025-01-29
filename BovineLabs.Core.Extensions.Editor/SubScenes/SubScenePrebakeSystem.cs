// <copyright file="SubScenePrebakeSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Editor.SubScenes
{
    using BovineLabs.Core.Editor.Settings;
    using Unity.Entities;
    using Unity.Scenes;
    using UnityEditor;
    using EditorSettings = BovineLabs.Core.Editor.Settings.EditorSettings;

    [WorldSystemFilter(WorldSystemFilterFlags.Editor)]
    public partial class SubScenePrebakeSystem : SystemBase
    {
        /// <inheritdoc />
        protected override void OnCreate()
        {
            foreach (var scene in EditorSettingsUtility.GetSettings<EditorSettings>().PrebakeScenes)
            {
                if (scene == null)
                {
                    continue;
                }

                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(scene, out var guid, out var id))
                {
                    continue;
                }

                SceneSystem.LoadSceneAsync(this.World.Unmanaged, new GUID(guid));
            }

            this.Enabled = false;
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
        }
    }
}
#endif
