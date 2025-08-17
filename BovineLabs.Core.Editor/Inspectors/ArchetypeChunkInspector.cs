// <copyright file="ArchetypeChunkInspector.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using Unity.Entities;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    internal class ArchetypeChunkInspector : PropertyInspector<ArchetypeChunk>
    {
        /// <inheritdoc/>
        public override VisualElement Build()
        {
            var chunk = new TextField
            {
                label = this.DisplayName,
                value = new IntPtr(this.Target.m_Chunk).ToString(),
            };

            chunk.SetEnabled(false);
            return chunk;
        }
    }
}
