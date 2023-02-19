// <copyright file="DisableRenderingHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_GRAPHICS
namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Rendering;

    /// <summary> Helper for disabling rendering in a hierarchy. </summary>
    public struct DisableRenderingHelper
    {
        [ReadOnly]
        private ComponentLookup<DisableRendering> disableRenderings;

        [ReadOnly]
        private BufferLookup<LinkedEntityGroup> linkedEntityGroup;

        [ReadOnly]
        private ComponentLookup<MaterialMeshInfo> materialMeshInfos;

        public DisableRenderingHelper(
            ComponentLookup<DisableRendering> disableRenderings,
            BufferLookup<LinkedEntityGroup> linkedEntityGroup,
            ComponentLookup<MaterialMeshInfo> materialMeshInfos)
        {
            this.disableRenderings = disableRenderings;
            this.linkedEntityGroup = linkedEntityGroup;
            this.materialMeshInfos = materialMeshInfos;
        }

        /// <summary> Toggles the <see cref="DisableRendering" /> component for all entities in a hierarchy. </summary>
        /// <param name="entityManager"> The entity manager. </param>
        /// <param name="entity"> The parent entity. </param>
        /// <param name="enabled"> When true adds <see cref="DisableRendering" /> otherwise removes it. </param>
        public static void SetWholeHierarchy(EntityManager entityManager, Entity entity, bool enabled)
        {
            if (!entityManager.HasComponent<LinkedEntityGroup>(entity))
            {
                ToggleRendering(entity);
            }
            else
            {
                foreach (var linked in entityManager.GetBuffer<LinkedEntityGroup>(entity).AsNativeArray())
                {
                    ToggleRendering(linked.Value);
                }
            }

            void ToggleRendering(Entity e)
            {
                // We traverse the whole hierarchy and set all the rendering states using MaterialMeshInfo to determine if it's a rendering component
                if (!entityManager.HasComponent<MaterialMeshInfo>(e))
                {
                    return;
                }

                var renderingDisabled = entityManager.HasComponent<DisableRendering>(e);
                if (enabled && renderingDisabled)
                {
                    entityManager.RemoveComponent<DisableRendering>(e);
                }
                else if (!enabled && !renderingDisabled)
                {
                    entityManager.AddComponent<DisableRendering>(e);
                }
            }
        }

        /// <summary> Toggles the <see cref="DisableRendering" /> component for all entities in a hierarchy. </summary>
        /// <param name="commandBuffer"> Command buffer. </param>
        /// <param name="entity"> The parent entity. </param>
        /// <param name="enabled"> When true adds <see cref="DisableRendering" /> otherwise removes it. </param>
        public void SetWholeHierarchy(EntityCommandBuffer commandBuffer, Entity entity, bool enabled)
        {
            if (!this.linkedEntityGroup.TryGetBuffer(entity, out var linkedEntityGroupBuffer))
            {
                this.ToggleRendering(commandBuffer, entity, enabled);
            }
            else
            {
                foreach (var linked in linkedEntityGroupBuffer.AsNativeArray())
                {
                    this.ToggleRendering(commandBuffer, linked.Value, enabled);
                }
            }
        }

        /// <summary> Toggles the <see cref="DisableRendering" /> component for all entities in a hierarchy. </summary>
        /// <param name="commandBuffer"> Parallel command buffer. </param>
        /// <param name="sortIndex"> Index for the parallel command buffer. </param>
        /// <param name="entity"> The parent entity. </param>
        /// <param name="enabled"> When true adds <see cref="DisableRendering" /> otherwise removes it. </param>
        public void SetWholeHierarchy(EntityCommandBuffer.ParallelWriter commandBuffer, int sortIndex, Entity entity, bool enabled)
        {
            // We traverse the whole hierarchy and set all the rendering states using MaterialMeshInfo to determine if it's a rendering component
            if (!this.linkedEntityGroup.TryGetBuffer(entity, out var linkedEntityGroupBuffer))
            {
                this.ToggleRendering(commandBuffer, sortIndex, entity, enabled);
            }
            else
            {
                foreach (var linked in linkedEntityGroupBuffer.AsNativeArray())
                {
                    this.ToggleRendering(commandBuffer, sortIndex, linked.Value, enabled);
                }
            }
        }

        private void ToggleRendering(EntityCommandBuffer commandBuffer, Entity entity, bool enabled)
        {
            // We traverse the whole hierarchy and set all the rendering states using MaterialMeshInfo to determine if it's a rendering component
            if (!this.materialMeshInfos.HasComponent(entity))
            {
                return;
            }

            var renderingDisabled = this.disableRenderings.HasComponent(entity);
            if (enabled && renderingDisabled)
            {
                commandBuffer.RemoveComponent<DisableRendering>(entity);
            }
            else if (!enabled && !renderingDisabled)
            {
                commandBuffer.AddComponent<DisableRendering>(entity);
            }
        }

        private void ToggleRendering(EntityCommandBuffer.ParallelWriter commandBuffer, int sortIndex, Entity entity, bool enabled)
        {
            // We traverse the whole hierarchy and set all the rendering states using MaterialMeshInfo to determine if it's a rendering component
            if (!this.materialMeshInfos.HasComponent(entity))
            {
                return;
            }

            var renderingDisabled = this.disableRenderings.HasComponent(entity);
            if (enabled && renderingDisabled)
            {
                commandBuffer.RemoveComponent<DisableRendering>(sortIndex, entity);
            }
            else if (!enabled && !renderingDisabled)
            {
                commandBuffer.AddComponent<DisableRendering>(sortIndex, entity);
            }
        }
    }
}
#endif
