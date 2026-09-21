namespace BovineLabs.Core.Authoring
{
    using System;
    using Unity.Entities;
    using UnityEngine;

    public class TagAuthoring : MonoBehaviour
    {
        public ComponentAsset[] Components = Array.Empty<ComponentAsset>();

        private class TagBaker : Baker<TagAuthoring>
        {
            public override void Bake(TagAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);

                foreach (var c in authoring.Components)
                {
                    if (c == null)
                    {
                        continue;
                    }

                    DependsOn(c);

                    AddComponent(entity, c.ResolveType());
                }
            }
        }
    }
}
