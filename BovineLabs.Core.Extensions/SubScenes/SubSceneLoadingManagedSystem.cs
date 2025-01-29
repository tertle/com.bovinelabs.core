// <copyright file="SubSceneLoadingManagedSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using System.Collections.Generic;
    using BovineLabs.Core.Groups;
    using Unity.Entities;
    using Unity.Scenes;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Hash128 = Unity.Entities.Hash128;

    [UpdateInGroup(typeof(AfterSceneSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation | Worlds.Service)]
    [UpdateBefore(typeof(SubSceneLoadingSystem))]
    public partial class SubSceneLoadingManagedSystem : SystemBase
    {
#if UNITY_EDITOR
        private readonly Dictionary<Hash128, SubScene> loading = new();
#endif

        /// <inheritdoc />
        protected override void OnCreate()
        {
            SceneManager.sceneLoaded += this.OnSceneLoaded;
        }

        /// <inheritdoc />
        protected override void OnDestroy()
        {
            SceneManager.sceneLoaded -= this.OnSceneLoaded;
        }

        /// <inheritdoc />
        protected override void OnStartRunning()
        {
            this.LoadAllExistingSubScenes();
        }

        /// <inheritdoc />
        protected override void OnUpdate()
        {
#if UNITY_EDITOR
            if (this.loading.Count == 0)
            {
                return;
            }

            var query = SystemAPI.QueryBuilder().WithAll<SceneReference>().Build();
            var entities = query.ToEntityArray(this.WorldUpdateAllocator);
            var subsceneReferences = query.ToComponentDataArray<SceneReference>(this.WorldUpdateAllocator);
            for (var i = 0; i < entities.Length; i++)
            {
                if (!this.loading.Remove(subsceneReferences[i].SceneGUID, out var subScene))
                {
                    continue;
                }

                this.EntityManager.AddComponentObject(entities[i], subScene);
            }
#endif
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            var subScenes = Object.FindObjectsByType<SubSceneLoadConfig>(FindObjectsSortMode.None);

            var entity = this.EntityManager.CreateEntity(typeof(SubSceneLoad));
            var subSceneLoad = this.EntityManager.GetBuffer<SubSceneLoad>(entity);

            foreach (var subSceneLoadConfig in subScenes)
            {
                // Only load scenes that were found in the loaded scene
                if (subSceneLoadConfig.gameObject.scene != scene)
                {
                    continue;
                }

                this.Load(subSceneLoad, subSceneLoadConfig);
            }
        }

        private void LoadAllExistingSubScenes()
        {
            var subScenes = Object.FindObjectsByType<SubSceneLoadConfig>(FindObjectsSortMode.None);

            var entity = this.EntityManager.CreateEntity(typeof(SubSceneLoad));
            var subSceneLoad = this.EntityManager.GetBuffer<SubSceneLoad>(entity);

            foreach (var subSceneLoadConfig in subScenes)
            {
                this.Load(subSceneLoad, subSceneLoadConfig);
            }
        }

        private void Load(DynamicBuffer<SubSceneLoad> subSceneLoad, SubSceneLoadConfig subSceneLoadConfig)
        {
            var s = subSceneLoadConfig.GetSubSceneLoad();
            if (!s.Scene.IsReferenceValid)
            {
                return;
            }

            subSceneLoad.Add(s);

#if UNITY_EDITOR
            this.loading.Add(s.Scene.Id.GlobalId.AssetGUID, subSceneLoadConfig.GetComponent<SubScene>());
#endif
        }
    }
}
#endif