// <copyright file="AssetLoadAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Authoring.SubScenes
{
    using System;
    using BovineLabs.Core.SubScenes;
    using Unity.Entities;
    using UnityEngine;

    public class AssetLoadAuthoring : MonoBehaviour
    {
        [SerializeField]
        private Data[] assets = Array.Empty<Data>();

        private class Baker : Baker<AssetLoadAuthoring>
        {
            public override void Bake(AssetLoadAuthoring authoring)
            {
                DynamicBuffer<AssetLoad> buffer = default;

                foreach (var s in authoring.assets)
                {
                    if (s.Asset == null)
                    {
                        continue;
                    }

                    if (s.Asset.scene.name != null)
                    {
                        Debug.LogError($"Trying to load a non-prefab asset {s.Asset}");
                        continue;
                    }

#if UNITY_SERVER
                    if ((s.TargetWorld & SubSceneLoadFlags.Service) != 0 && s.IgnoreOnDedicated)
                    {
                        continue;
                    }
#endif

                    if (!this.IncludeScene(s.TargetWorld))
                    {
                        continue;
                    }

                    if (!buffer.IsCreated)
                    {
                        buffer = this.AddBuffer<AssetLoad>(this.GetEntity(TransformUsageFlags.None));
                    }

                    // Depends on shouldn't be required as we don't care if the scene asset actually changes, only if our own references change
                    buffer.Add(new AssetLoad
                    {
                        Asset = s.Asset,
                        TargetWorld = SubSceneLoadUtil.ConvertFlags(s.TargetWorld),
                    });
                }
            }
        }

        [Serializable]
        private class Data
        {
            public GameObject? Asset;

            public SubSceneLoadFlags TargetWorld = SubSceneLoadFlags.Service;
        }
    }
}
#endif
