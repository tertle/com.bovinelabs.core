// <copyright file="SubSceneLoadAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.Authoring.SubScenes
{
    using BovineLabs.Core.Authoring.EntityCommands;
    using BovineLabs.Core.SubScenes;
    using Unity.Entities;
    using UnityEngine;

    public class SubSceneLoadAuthoring : MonoBehaviour
    {
        [SerializeField]
        private SubSceneSettings? settings;

        private class Baker : Baker<SubSceneLoadAuthoring>
        {
            public override void Bake(SubSceneLoadAuthoring authoring)
            {
                if (!authoring.settings)
                {
                    BLGlobalLogger.LogErrorString("SubSceneSettings not assigned");
                    return;
                }

                this.BakeSubScenes(authoring.settings);
                this.BakeAssets(authoring.settings);
            }

            private void BakeSubScenes(SubSceneSettings settings)
            {
                foreach (var set in settings.SceneSets)
                {
                    if (!this.IncludeScene(set.TargetWorld))
                    {
                        continue;
                    }

                    var entity = this.CreateAdditionalEntity(TransformUsageFlags.None);

                    var commands = new BakerCommands(this, entity);
                    SubSceneAuthUtil.AddComponents(ref commands, set);
                }
            }

            private void BakeAssets(SubSceneSettings settings)
            {
                var buffer = this.AddBuffer<AssetLoad>(this.GetEntity(TransformUsageFlags.None));

                foreach (var s in settings.AssetSets)
                {
                    if (!this.IncludeScene(s.TargetWorld))
                    {
                        continue;
                    }

                    var targetWorld = SubSceneLoadUtil.ConvertFlags(s.TargetWorld);

                    foreach (var asset in s.Assets)
                    {
                        if (asset == null)
                        {
                            continue;
                        }

                        // Depends on shouldn't be required as we don't care if the scene asset actually changes, only if our own references change
                        buffer.Add(new AssetLoad
                        {
                            Asset = asset,
                            TargetWorld = targetWorld,
                        });
                    }
                }
            }
        }
    }
}
#endif
