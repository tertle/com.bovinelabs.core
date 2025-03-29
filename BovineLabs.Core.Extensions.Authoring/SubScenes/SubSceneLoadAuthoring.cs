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
                if (authoring.settings == null)
                {
                    Debug.LogError("SuBSceneSettings not assigned");
                    return;
                }

                foreach (var set in authoring.settings.SceneSets)
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
        }
    }
}
#endif
