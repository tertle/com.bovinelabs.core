// <copyright file="SubSceneAuthUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Authoring.SubScenes
{
    using System.Collections.Generic;
    using BovineLabs.Core.EntityCommands;
    using BovineLabs.Core.SubScenes;
    using Unity.Entities.Serialization;
    using UnityEditor;

    public static class SubSceneAuthUtil
    {
        public static void AddComponents<T>(ref T commands, SubSceneSet set)
            where T : IEntityCommands
        {
            AddComponents(ref commands, new SubSceneSetId(set.ID), set.TargetWorld, set.IsRequired, set.WaitForLoad, set.AutoLoad, set.Scenes);
        }

        public static void AddComponents<T>(
            ref T commands, SubSceneSetId id, SubSceneLoadFlags targetWorld, bool isRequired, bool waitForLoad, bool autoLoad, List<SceneAsset> scenes)
            where T : IEntityCommands
        {
            commands.AddComponent(new SubSceneLoadData
            {
                ID = id,
                WaitForLoad = waitForLoad || isRequired, // Is required should always wait
                TargetWorld = SubSceneLoadUtil.ConvertFlags(targetWorld),
                IsRequired = isRequired,
            });

            commands.AddComponent<LoadSubScene>(); // Is required should always default load
            commands.SetComponentEnabled<LoadSubScene>(autoLoad || isRequired); // Is required should always default load

            commands.AddComponent<SubSceneLoaded>(); // Is required should always default load
            commands.SetComponentEnabled<SubSceneLoaded>(false); // Is required should always default load

            commands.AddBuffer<SubSceneEntity>(); // Is required should always default load
            commands.SetComponentEnabled<SubSceneEntity>(false); // Is required should always default load

            var sceneLoad = commands.AddBuffer<SubSceneBuffer>();
            foreach (var sceneAsset in scenes)
            {
                if (sceneAsset == null)
                {
                    continue;
                }

                sceneLoad.Add(new SubSceneBuffer
                {
                    Name = sceneAsset.name,
                    Scene = new EntitySceneReference(sceneAsset),
                });
            }
        }
    }
}
#endif
