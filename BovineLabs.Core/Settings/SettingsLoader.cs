// <copyright file="SettingsLoader.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Reflection;
    using System.Threading.Tasks;
    using BovineLabs.Core.ResourceManagement;
    using Unity.Collections;
    using Unity.Entities;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.Assertions;
    using Debug = UnityEngine.Debug;

    /// <summary> Manages loading settings. </summary>
    public class SettingsLoader
    {
        private readonly List<ISettings> defaultSettings = new List<ISettings>();
        private readonly List<ISettings> serverSettings = new List<ISettings>();
        private readonly List<ISettings> clientSettings = new List<ISettings>();

        private readonly AssetLabelReference label;

        /// <summary> Initializes a new instance of the <see cref="SettingsLoader" /> class. </summary>
        /// <param name="label">The settings label.</param>
        public SettingsLoader(AssetLabelReference label)
        {
            this.label = label;
        }

        /// <summary> Load all settings. </summary>
        /// <returns> The task. </returns>
        public async Task LoadAll()
        {
            Assert.AreEqual(0, this.defaultSettings.Count, "Settings already loaded");
            Assert.AreEqual(0, this.clientSettings.Count, "Settings already loaded");
            Assert.AreEqual(0, this.serverSettings.Count, "Settings already loaded");

            var settings = await AssetManager.LoadAssetsAsync<ScriptableObject>(this.label);

            foreach (var so in settings)
            {
                if (so is ISettings setting)
                {
                    var targetWorld = GetAttribute<ExecuteInWorld>(so.GetType());
                    var world = targetWorld?.World ?? Worlds.ClientAndServer;

                    if (world == Worlds.Default || (world & Worlds.DefaultExplicit) != 0)
                    {
                        this.defaultSettings.Add(setting);
                    }

                    if ((world & Worlds.Client) != 0)
                    {
                        this.clientSettings.Add(setting);
                    }

                    if ((world & Worlds.Server) != 0)
                    {
                        this.serverSettings.Add(setting);
                    }
                }
                else
                {
                    Debug.LogError($"ScriptableObject with label {this.label} not an {nameof(ISettings)}");
                }
            }
        }

        /// <summary> Add settings to a world. </summary>
        /// <param name="world"> The world to add settings to. </param>
        /// <param name="worlds"> The type of settings to add. </param>
        public void AddSettingsToWorld(World world, Worlds worlds)
        {
            List<ISettings> settings;
            switch (worlds)
            {
                case Worlds.Default:
                    settings = this.defaultSettings;
                    break;
                case Worlds.Client:
                    settings = this.clientSettings;
                    break;
                case Worlds.Server:
                    settings = this.serverSettings;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(worlds), worlds, null);
            }

            if (settings.Count > 0)
            {
                var settingEntity = world.EntityManager.CreateEntity();
#if UNITY_EDITOR
                world.EntityManager.SetName(settingEntity, "Settings");
#endif
                foreach (var setting in settings)
                {
                    setting.Convert(world.EntityManager, settingEntity);
                }
            }

            ValidateServerComponents(world, worlds);
        }

        private static T GetAttribute<T>(ICustomAttributeProvider systemType)
            where T : Attribute
        {
            var attribs = systemType.GetCustomAttributes(typeof(T), true);
            if (attribs.Length != 1)
            {
                return null;
            }

            return attribs[0] as T;
        }

        [Conditional("UNITY_EDITOR")]
        private static void ValidateServerComponents(World world, Worlds worlds)
        {
            if (worlds != Worlds.Server)
            {
                return;
            }

            var archetypes = new NativeList<EntityArchetype>(Allocator.Temp);
            world.EntityManager.GetAllArchetypes(archetypes);

            foreach (var archetype in archetypes)
            {
                var components = archetype.GetComponentTypes();
                foreach (var component in components)
                {
                    if (!component.IsManagedComponent)
                    {
                        continue;
                    }

                    Debug.LogError("Managed settings added to server. This is not allowed.");
                    return;
                }
            }
        }
    }
}