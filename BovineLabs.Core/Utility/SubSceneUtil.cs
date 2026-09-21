namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Scenes;

    public struct SubSceneUtil
    {
        [ReadOnly]
        private ComponentLookup<SceneReference> _sceneReferences;

        [ReadOnly]
        private BufferLookup<ResolvedSectionEntity> _resolvedSectionEntitys;

        [ReadOnly]
        private ComponentLookup<SceneSectionStreamingSystem.StreamingState> _streamingStates;

        public SubSceneUtil(ref SystemState state)
        {
            _sceneReferences = state.GetComponentLookup<SceneReference>(true);
            _resolvedSectionEntitys = state.GetBufferLookup<ResolvedSectionEntity>(true);
            _streamingStates = state.GetComponentLookup<SceneSectionStreamingSystem.StreamingState>(true);
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

        public static bool IsSectionPendingUnload(ref SystemState state, Entity sectionEntity)
        {
            while (true)
            {
                if (!state.EntityManager.HasComponent<RequestSceneLoaded>(sectionEntity))
                {
                    return true;
                }

                if (!state.EntityManager.HasComponent<SceneTag>(sectionEntity))
                {
                    return false;
                }

                // Unity tags nested section metadata with the section that contains its scene.
                sectionEntity = state.EntityManager.GetSharedComponent<SceneTag>(sectionEntity).SceneEntity;
            }
        }

        public void Update(ref SystemState state)
        {
            _sceneReferences.Update(ref state);
            _resolvedSectionEntitys.Update(ref state);
            _streamingStates.Update(ref state);
        }

        public bool IsSceneLoaded(Entity entity)
        {
            if (!_sceneReferences.HasComponent(entity))
            {
                return false;
            }

            if (!_resolvedSectionEntitys.HasBuffer(entity))
            {
                return false;
            }

            var resolvedSectionEntities = _resolvedSectionEntitys[entity];

            if (resolvedSectionEntities.Length == 0)
            {
                return false;
            }

            foreach (var s in resolvedSectionEntities)
            {
                if (!IsSectionLoaded(s.SectionEntity))
                {
                    return false;
                }
            }

            return true;
        }

        public bool IsSectionLoaded(Entity sectionEntity)
        {
            if (!_streamingStates.TryGetComponent(sectionEntity, out var status))
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
