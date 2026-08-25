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
        [ReadOnly]
        private ComponentLookup<SceneReference> sceneReferences;

        [ReadOnly]
        private BufferLookup<ResolvedSectionEntity> resolvedSectionEntitys;

        [ReadOnly]
        private ComponentLookup<SceneSectionStreamingSystem.StreamingState> streamingStates;

        public SubSceneUtil(ref SystemState state)
        {
            this.sceneReferences = state.GetComponentLookup<SceneReference>(true);
            this.resolvedSectionEntitys = state.GetBufferLookup<ResolvedSectionEntity>(true);
            this.streamingStates = state.GetComponentLookup<SceneSectionStreamingSystem.StreamingState>(true);
        }

        public static bool IsSectionLoaded(ref SystemState state, Entity sectionEntity)
        {
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
            if (!this.streamingStates.TryGetComponent(sectionEntity, out var status))
            {
                return false;
            }

            return status.Status == SceneSectionStreamingSystem.StreamingStatus.Loaded;
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
