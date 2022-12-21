// <copyright file="DisableRenderingHelper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_GRAPHICS
namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Rendering;
    using Unity.Transforms;

    /// <summary> Helper for disabling rendering in a hierarchy. </summary>
    public struct DisableRenderingHelper
    {
        [ReadOnly]
        private ComponentLookup<DisableRendering> disableRenderings;

        [ReadOnly]
        private BufferLookup<Child> childrens;

        [ReadOnly]
        private ComponentLookup<RenderBounds> renderBounds;

        public DisableRenderingHelper(
            ComponentLookup<DisableRendering> disableRenderings,
            BufferLookup<Child> childrens,
            ComponentLookup<RenderBounds> renderBounds)
        {
            this.disableRenderings = disableRenderings;
            this.childrens = childrens;
            this.renderBounds = renderBounds;
        }

        /// <summary> Toggles the <see cref="DisableRendering" /> component for all entities in a hierarchy. </summary>
        /// <param name="commandBuffer"> Command buffer. </param>
        /// <param name="entity"> The parent entity. </param>
        /// <param name="enabled"> When true adds <see cref="DisableRendering" /> otherwise removes it. </param>
        public void SetWholeHierarchy(EntityCommandBuffer commandBuffer, Entity entity, bool enabled)
        {
            // We traverse the whole hierarchy and set all the rendering states using RenderBounds to determine if it's a rendering component
            if (this.renderBounds.HasComponent(entity))
            {
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

            if (!this.childrens.HasBuffer(entity))
            {
                return;
            }

            var children = this.childrens[entity];
            for (var i = 0; i < children.Length; i++)
            {
                this.SetWholeHierarchy(commandBuffer, children[i].Value, enabled);
            }
        }

        /// <summary> Toggles the <see cref="DisableRendering" /> component for all entities in a hierarchy. </summary>
        /// <param name="commandBuffer"> Parallel command buffer. </param>
        /// <param name="sortIndex"> Index for the parallel command buffer. </param>
        /// <param name="entity"> The parent entity. </param>
        /// <param name="enabled"> When true adds <see cref="DisableRendering" /> otherwise removes it. </param>
        public void SetWholeHierarchy(EntityCommandBuffer.ParallelWriter commandBuffer, int sortIndex, Entity entity, bool enabled)
        {
            // We traverse the whole hierarchy and set all the rendering states using RenderBounds to determine if it's a rendering component
            if (this.renderBounds.HasComponent(entity))
            {
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

            if (!this.childrens.HasBuffer(entity))
            {
                return;
            }

            var children = this.childrens[entity];
            for (var i = 0; i < children.Length; i++)
            {
                this.SetWholeHierarchy(commandBuffer, sortIndex, children[i].Value, enabled);
            }
        }
    }
}
#endif
