// <copyright file="SubSceneLoadingManagedSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using System.Collections.Generic;
    using BovineLabs.Core.Groups;
    using Unity.Entities;
    using Unity.Entities.Serialization;
    using Unity.Scenes;
    using UnityEngine;
    using UnityEngine.SceneManagement;
    using Hash128 = Unity.Entities.Hash128;

    [UpdateInGroup(typeof(AfterSceneSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(Worlds.SimulationThinService)]
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
            var subScenes = Object.FindObjectsByType<SubScene>(FindObjectsSortMode.None);

            foreach (var subScene in subScenes)
            {
                // Only load scenes that were found in the loaded scene
                if (subScene.gameObject.scene != scene)
                {
                    continue;
                }

                this.LoadSubScene(subScene);
            }
        }

        private void LoadAllExistingSubScenes()
        {
            var subScenes = Object.FindObjectsByType<SubScene>(FindObjectsSortMode.None);

            foreach (var subScene in subScenes)
            {
                this.LoadSubScene(subScene);
            }
        }

        private void LoadSubScene(SubScene subScene)
        {
            if (subScene.SceneGUID == default)
            {
                return;
            }

            var entity = this.EntityManager.CreateEntity(typeof(SubSceneLoadData), typeof(SubSceneEntity), typeof(SubSceneBuffer), typeof(LoadSubScene),
                typeof(SubSceneLoaded));

            this.EntityManager.SetComponentData(entity, new SubSceneLoadData
            {
                ID = -1,
                IsRequired = subScene.AutoLoadScene,
                WaitForLoad = subScene.AutoLoadScene,
                TargetWorld = this.World.Flags,
            });

            this.EntityManager.SetComponentEnabled<LoadSubScene>(entity, subScene.AutoLoadScene);
            this.EntityManager.SetComponentEnabled<SubSceneLoaded>(entity, false);
            this.EntityManager.SetComponentEnabled<SubSceneEntity>(entity, false);

            var subSceneLoad = this.EntityManager.GetBuffer<SubSceneBuffer>(entity);
            subSceneLoad.Add(new SubSceneBuffer
            {
#if UNITY_EDITOR
                Name = subScene.SceneName,
#else
                    Name = subScene.name,
#endif
                Scene = new EntitySceneReference(subScene.SceneGUID, 0),
            });

#if UNITY_EDITOR
            this.loading.Add(subScene.SceneGUID, subScene);
#endif
        }
    }
}
#endif
