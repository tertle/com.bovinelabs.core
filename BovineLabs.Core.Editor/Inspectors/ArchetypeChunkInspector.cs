namespace BovineLabs.Core.Editor.Inspectors
{
    using System;
    using Unity.Entities;
    using Unity.Entities.UI;
    using UnityEngine.UIElements;

    internal class ArchetypeChunkInspector : PropertyInspector<ArchetypeChunk>
    {
        public override VisualElement Build()
        {
            var chunk = new TextField
            {
                label = DisplayName,
                value = new IntPtr(Target.m_Chunk).ToString(),
            };

            chunk.SetEnabled(false);
            return chunk;
        }
    }
}
