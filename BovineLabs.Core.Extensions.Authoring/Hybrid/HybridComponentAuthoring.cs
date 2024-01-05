// <copyright file="HybridComponentAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_HYBRID
namespace BovineLabs.Core.Authoring.Hybrid
{
    using BovineLabs.Core.Hybrid;
    using Unity.Entities;
    using UnityEngine;

    public class HybridComponentAuthoring : MonoBehaviour
    {
        [SerializeField]
        public GameObject? Prefab;

        public class HybridComponentBaker : Baker<HybridComponentAuthoring>
        {
            public override void Bake(HybridComponentAuthoring authoring)
            {
                if (authoring.Prefab == null)
                {
                    Debug.LogError("Prefab not setup");
                    return;
                }

                var entity = this.GetEntity(TransformUsageFlags.None);
                this.AddComponentObject(entity, new HybridComponent { Value = authoring.Prefab });
            }
        }
    }
}
#endif
