// <copyright file="LibraryBootstrap.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_BOOTSTRAP
namespace BovineLabs.Core.Editor
{
    using Unity.Entities;
    using UnityEditor;

    /// <summary> For standalone libraries certain things need to be setup for testing. </summary>
    public class LibraryBootstrap : ICustomBootstrap
    {
        /// <inheritdoc/>
        public bool Initialize(string defaultWorldName)
        {
            var world = new World(defaultWorldName, WorldFlags.Game);
            World.DefaultGameObjectInjectionWorld = world;

            InjectSettings(world);

            var systemList = DefaultWorldInitialization.GetAllSystems(WorldSystemFilterFlags.Default);
            DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(world, systemList);
            ScriptBehaviourUpdateOrder.AddWorldToCurrentPlayerLoop(world);
            return true;
        }

        private static void InjectSettings(World world)
        {
            var settingEntity = world.EntityManager.CreateEntity();
            world.EntityManager.SetName(settingEntity, "Settings");

            var settingGuids = AssetDatabase.FindAssets("t:settings");

            foreach (var guid in settingGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var setting = AssetDatabase.LoadAssetAtPath<Core.Settings.Settings>(path);

                if (setting == null)
                {
                    continue;
                }

                setting.Convert(world.EntityManager, settingEntity);
            }
        }
    }
}
#endif