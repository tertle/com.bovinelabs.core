// <copyright file="SubSceneUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;

    public struct SubSceneUtility
    {
        [ReadOnly]
        private ComponentLookup<SceneReference> sceneReferences;

        [ReadOnly]
        private BufferLookup<ResolvedSectionEntity> resolvedSectionEntitys;

        [ReadOnly]
        private ComponentLookup<SceneSectionStreamingSystem.StreamingState> streamingStates;

        [ReadOnly]
        private ComponentLookup<RequestSceneLoaded> requestSceneLoadeds;

        public SubSceneUtility(ref SystemState state)
        {
            this.sceneReferences = state.GetComponentLookup<SceneReference>(true);
            this.resolvedSectionEntitys = state.GetBufferLookup<ResolvedSectionEntity>(true);
            this.streamingStates = state.GetComponentLookup<SceneSectionStreamingSystem.StreamingState>(true);
            this.requestSceneLoadeds = state.GetComponentLookup<RequestSceneLoaded>(true);
        }

        public static void OpenScene(ref SystemState state, Entity entity)
        {
            if (state.EntityManager.HasComponent<RequestSceneLoaded>(entity))
            {
                Debug.LogWarning("Trying to open SubScene that is already open.");
                return;
            }

            state.EntityManager.AddComponent<RequestSceneLoaded>(entity);

            var resolvedSectionEntities = state.EntityManager.GetBuffer<ResolvedSectionEntity>(entity);
            foreach (var section in resolvedSectionEntities.ToNativeArray(Allocator.Temp))
            {
                state.EntityManager.AddComponent<RequestSceneLoaded>(section.SectionEntity);
            }
        }

        public static void CloseScene(ref SystemState state, Entity entity)
        {
            if (!state.EntityManager.HasComponent<RequestSceneLoaded>(entity))
            {
                Debug.LogWarning("Trying to close SubScene that isn't open.");
                return;
            }

            state.EntityManager.RemoveComponent<RequestSceneLoaded>(entity);

            var resolvedSectionEntities = state.EntityManager.GetBuffer<ResolvedSectionEntity>(entity);

            foreach (var section in resolvedSectionEntities.ToNativeArray(Allocator.Temp))
            {
                if (state.EntityManager.HasComponent<RequestSceneLoaded>(section.SectionEntity))
                {
                    state.EntityManager.RemoveComponent<RequestSceneLoaded>(section.SectionEntity);
                }
            }
        }

        /// <summary> Check if a subscene is loaded. </summary>
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
            if (!this.streamingStates.HasComponent(sectionEntity))
            {
                return false;
            }

            return this.streamingStates[sectionEntity].Status == SceneSectionStreamingSystem.StreamingStatus.Loaded;
        }

        public void CloseScene(EntityCommandBuffer ecb, Entity entity)
        {
            if (!this.requestSceneLoadeds.HasComponent(entity))
            {
                Debug.LogWarning("Trying to close SubScene that isn't open.");
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

        public void OpenScene(EntityCommandBuffer ecb, Entity entity)
        {
            if (this.requestSceneLoadeds.HasComponent(entity))
            {
                Debug.LogWarning("Trying to open SubScene that is already open.");
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
}
