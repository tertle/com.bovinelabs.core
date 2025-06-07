// <copyright file="SubSceneUtil.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;

    public struct SubSceneUtil
    {
        public enum StreamingStatus
        {
            None,
            Loading,
            Loaded,
        }

        [ReadOnly]
        private ComponentLookup<SceneReference> sceneReferences;

        [ReadOnly]
        private BufferLookup<ResolvedSectionEntity> resolvedSectionEntitys;

        [ReadOnly]
        private ComponentLookup<SceneSectionStreamingSystem.StreamingState> streamingStates;

        [ReadOnly]
        private ComponentLookup<RequestSceneLoaded> requestSceneLoadeds;

        public SubSceneUtil(ref SystemState state)
        {
            this.sceneReferences = state.GetComponentLookup<SceneReference>(true);
            this.resolvedSectionEntitys = state.GetBufferLookup<ResolvedSectionEntity>(true);
            this.streamingStates = state.GetComponentLookup<SceneSectionStreamingSystem.StreamingState>(true);
            this.requestSceneLoadeds = state.GetComponentLookup<RequestSceneLoaded>(true);
        }

        public static void LoadScene(ref SystemState state, Entity entity)
        {
            if (state.EntityManager.HasComponent<RequestSceneLoaded>(entity))
            {
                BLGlobalLogger.LogWarning("Trying to open SubScene that is already open.");
                return;
            }

            state.EntityManager.AddComponent<RequestSceneLoaded>(entity);

            var resolvedSectionEntities = state.EntityManager.GetBuffer<ResolvedSectionEntity>(entity);
            foreach (var section in resolvedSectionEntities.ToNativeArray(Allocator.Temp))
            {
                OpenSection(ref state, section.SectionEntity);
            }
        }

        public static void OpenSection(ref SystemState state, Entity section)
        {
            state.EntityManager.AddComponent<RequestSceneLoaded>(section);
        }

        public static void UnloadScene(ref SystemState state, Entity entity)
        {
            if (!state.EntityManager.HasComponent<RequestSceneLoaded>(entity))
            {
                BLGlobalLogger.LogWarning("Trying to close SubScene that isn't open.");
                return;
            }

            state.EntityManager.RemoveComponent<RequestSceneLoaded>(entity);

            var resolvedSectionEntities = state.EntityManager.GetBuffer<ResolvedSectionEntity>(entity);

            foreach (var section in resolvedSectionEntities.ToNativeArray(Allocator.Temp))
            {
                UnloadScene(ref state, section.SectionEntity);
            }
        }

        public static void CloseSection(ref SystemState state, Entity section)
        {
            if (state.EntityManager.HasComponent<RequestSceneLoaded>(section))
            {
                state.EntityManager.RemoveComponent<RequestSceneLoaded>(section);
            }
        }

        /// <summary> Check if a subscene is loaded. </summary>
        /// <param name="state"> The executing wrld. </param>
        /// <param name="entity"> The entity with the loading component data.  This is the entity returned by LoadSceneAsync. </param>
        /// <returns> True if the scene is loaded. </returns>
        public static bool IsSceneLoaded(ref SystemState state, Entity entity)
        {
            if (!state.EntityManager.HasComponent<SceneReference>(entity))
            {
                return false;
            }

            if (!state.EntityManager.HasBuffer<ResolvedSectionEntity>(entity))
            {
                return false;
            }

            var resolvedSectionEntities = state.EntityManager.GetBuffer<ResolvedSectionEntity>(entity);
            if (resolvedSectionEntities.Length == 0)
            {
                return false;
            }

            foreach (var s in resolvedSectionEntities)
            {
                if (!IsSectionLoaded(ref state, s.SectionEntity))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsLoadingOrLoaded(ref SystemState state, Entity entity)
        {
            if (!state.EntityManager.HasComponent<SceneReference>(entity))
            {
                return false;
            }

            if (!state.EntityManager.HasComponent<ResolvedSectionEntity>(entity))
            {
                return false;
            }

            var resolvedSectionEntities = state.EntityManager.GetBuffer<ResolvedSectionEntity>(entity);

            if (resolvedSectionEntities.Length == 0)
            {
                return false;
            }

            foreach (var s in resolvedSectionEntities)
            {
                if (!IsSectionLoadingOrLoaded(ref state, s.SectionEntity))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool IsSectionLoaded(ref SystemState state, Entity sectionEntity)
        {
            // return true;
            if (!state.EntityManager.HasComponent<SceneSectionStreamingSystem.StreamingState>(sectionEntity))
            {
                return false;
            }

            var streamingState = state.EntityManager.GetComponentData<SceneSectionStreamingSystem.StreamingState>(sectionEntity);
            return streamingState.Status == SceneSectionStreamingSystem.StreamingStatus.Loaded;
        }

        public static bool IsSectionLoadingOrLoaded(ref SystemState state, Entity sectionEntity)
        {
            return state.EntityManager.HasComponent<RequestSceneLoaded>(sectionEntity) ||
                state.EntityManager.HasComponent<SceneSectionStreamingSystem.StreamingState>(sectionEntity);
        }

        public void Update(ref SystemState state)
        {
            this.sceneReferences.Update(ref state);
            this.resolvedSectionEntitys.Update(ref state);
            this.streamingStates.Update(ref state);
            this.requestSceneLoadeds.Update(ref state);
        }

        /// <summary> Check if a subscene is loaded. </summary>
        /// <param name="entity"> The entity with the loading component data.  This is the entity returned by LoadSceneAsync. </param>
        /// <returns> True if the scene is loaded. </returns>
        public bool IsSceneLoaded(Entity entity)
        {
            if (!this.sceneReferences.HasComponent(entity))
            {
                return false;
            }

            if (!this.resolvedSectionEntitys.HasBuffer(entity))
            {
                return false;
            }

            var resolvedSectionEntities = this.resolvedSectionEntitys[entity];

            if (resolvedSectionEntities.Length == 0)
            {
                return false;
            }

            foreach (var s in resolvedSectionEntities)
            {
                if (!this.IsSectionLoaded(s.SectionEntity))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsSceneLoadingOrLoaded(Entity entity)
        {
            if (!this.sceneReferences.HasComponent(entity))
            {
                return false;
            }

            if (!this.resolvedSectionEntitys.HasBuffer(entity))
            {
                return false;
            }

            var resolvedSectionEntities = this.resolvedSectionEntitys[entity];

            if (resolvedSectionEntities.Length == 0)
            {
                return false;
            }

            foreach (var s in resolvedSectionEntities)
            {
                if (!this.IsSectionLoadedOrLoading(s.SectionEntity))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary> Check if a subscene is loaded. </summary>
        /// <param name="entity"> The entity with the loading component data. This is the entity returned by LoadSceneAsync. </param>
        /// <returns> True if the scene is loaded. </returns>
        public StreamingStatus GetStreamingStatus(Entity entity)
        {
            if (!this.sceneReferences.HasComponent(entity))
            {
                return StreamingStatus.None;
            }

            if (!this.resolvedSectionEntitys.HasBuffer(entity))
            {
                return StreamingStatus.None;
            }

            var resolvedSectionEntities = this.resolvedSectionEntitys[entity];

            if (resolvedSectionEntities.Length == 0)
            {
                return StreamingStatus.None;
            }

            foreach (var s in resolvedSectionEntities)
            {
                if (!this.streamingStates.TryGetComponent(s.SectionEntity, out var status))
                {
                    return StreamingStatus.None;
                }

                if (status.Status != SceneSectionStreamingSystem.StreamingStatus.Loaded)
                {
                    return StreamingStatus.Loading;
                }
            }

            return StreamingStatus.Loaded;
        }

        /// <summary>
        /// Check if a section of a subscene is loaded.
        /// </summary>
        /// <param name="sectionEntity">
        /// The section entity representing the scene section. The section entities can be found in the ResolvedSectionEntity Buffer on the
        /// scene entity.
        /// </param>
        /// <returns> True if the scene section is loaded. </returns>
        public bool IsSectionLoaded(Entity sectionEntity)
        {
            // return true;
            if (!this.streamingStates.TryGetComponent(sectionEntity, out var status))
            {
                return false;
            }

            return status.Status == SceneSectionStreamingSystem.StreamingStatus.Loaded;
        }

        public bool IsSectionLoadedOrLoading(Entity sectionEntity)
        {
            return this.streamingStates.HasComponent(sectionEntity);
        }

        public void UnloadScene(EntityCommandBuffer ecb, Entity entity)
        {
            if (!this.requestSceneLoadeds.HasComponent(entity))
            {
                BLGlobalLogger.LogWarning("Trying to close SubScene that isn't open.");
                return;
            }

            ecb.RemoveComponent<RequestSceneLoaded>(entity);

            var resolvedSectionEntities = this.resolvedSectionEntitys[entity];

            foreach (var section in resolvedSectionEntities)
            {
                if (this.requestSceneLoadeds.HasComponent(section.SectionEntity))
                {
                    ecb.RemoveComponent<RequestSceneLoaded>(section.SectionEntity);
                }
            }
        }

        public void LoadScene(EntityCommandBuffer ecb, Entity entity)
        {
            if (this.requestSceneLoadeds.HasComponent(entity))
            {
                BLGlobalLogger.LogWarning("Trying to open SubScene that is already open.");
                return;
            }

            ecb.AddComponent<RequestSceneLoaded>(entity);

            var resolvedSectionEntities = this.resolvedSectionEntitys[entity];
            foreach (var section in resolvedSectionEntities)
            {
                ecb.AddComponent<RequestSceneLoaded>(section.SectionEntity);
            }
        }
    }

    public static class EntityQueryBuilderExtensions
    {
        public static EntityQueryBuilder WithSceneLoadRequest(this EntityQueryBuilder builder)
        {
            return builder
                .WithAll<RequestSceneLoaded, SceneSectionData, ResolvedSectionPath>()
                .WithNone<SceneSectionStreamingSystem.StreamingState, DisableSceneResolveAndLoad>();
        }

        public static EntityQueryBuilder WithSceneUnloadRequest(this EntityQueryBuilder builder)
        {
            return builder
                .WithAll<SceneSectionStreamingSystem.StreamingState, SceneSectionData, SceneEntityReference>()
                .WithNone<RequestSceneLoaded, DisableSceneResolveAndLoad>();
        }
    }
}
