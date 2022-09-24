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
        private ComponentDataFromEntity<SceneReference> sceneReferences;

        [ReadOnly]
        private BufferFromEntity<ResolvedSectionEntity> resolvedSectionEntitys;

        [ReadOnly]
        private ComponentDataFromEntity<SceneSectionStreamingSystem.StreamingState> streamingStates;

        [ReadOnly]
        private ComponentDataFromEntity<RequestSceneLoaded> requestSceneLoadeds;

        public SubSceneUtility(SystemBase systemBase)
        {
            this.sceneReferences = systemBase.GetComponentDataFromEntity<SceneReference>(true);
            this.resolvedSectionEntitys = systemBase.GetBufferFromEntity<ResolvedSectionEntity>(true);
            this.streamingStates = systemBase.GetComponentDataFromEntity<SceneSectionStreamingSystem.StreamingState>(true);
            this.requestSceneLoadeds = systemBase.GetComponentDataFromEntity<RequestSceneLoaded>(true);

            // TODO add Build() method/s
        }

        /// <summary> Check if a subscene is loaded. </summary>
        /// <param name="entity">The entity with the loading component data.  This is the entity returned by LoadSceneAsync.</param>
        /// <returns>True if the scene is loaded.</returns>
        public bool IsSceneLoaded(Entity entity)
        {
            if (!this.sceneReferences.HasComponent(entity))
            {
                return false;
            }

            if (!this.resolvedSectionEntitys.HasComponent(entity))
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
        /// <param name="sectionEntity">The section entity representing the scene section. The section entities can be found in the ResolvedSectionEntity Buffer on the scene entity.</param>
        /// <returns>True if the scene section is loaded.</returns>
        private bool IsSectionLoaded(Entity sectionEntity)
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
