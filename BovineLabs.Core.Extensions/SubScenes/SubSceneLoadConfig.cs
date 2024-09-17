// <copyright file="SubSceneLoadConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;
    using Unity.Entities.Serialization;
    using Unity.Mathematics;
    using Unity.Scenes;
    using UnityEngine;

    /// <summary> Marks a <see cref="SubScene" /> as only loading in a specific world type. </summary>
    [RequireComponent(typeof(SubScene))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1)] // just needs to execute before SubScene to stop autoload
    public class SubSceneLoadConfig : MonoBehaviour
    {
        [Tooltip("The target world to load the SubScene into.")]
        [SerializeField]
#if UNITY_NETCODE
        private SubSceneLoadFlags targetWorld = SubSceneLoadFlags.ThinClient | SubSceneLoadFlags.Client | SubSceneLoadFlags.Server;
#else
        private SubSceneLoadFlags targetWorld = SubSceneLoadFlags.Game;
#endif

        [Tooltip("The loading mode to load theSubScene.")]
        [SerializeField]
        private SubSceneLoadMode loadMode = SubSceneLoadMode.BoundingVolume;

        [SerializeField]
        private bool isRequired;

        [Min(0)]
        [Tooltip("The distance value for loading a BoundingVolume. A value less than or equal to zero will fall back to the GameConfig.LoadMaxDistance")]
        [SerializeField]
        private float loadMaxDistanceOverride;

        [Min(0)]
        [Tooltip("The distance value for unloading a BoundingVolume A value less than or equal to zero will fall back to the GameConfig.UnloadMaxDistance")]
        [SerializeField]
        private float unloadMaxDistanceOverride;

        public SubSceneLoad SubSceneLoad
        {
            get
            {
                var subScene = this.GetComponent<SubScene>();
                return new SubSceneLoad
                {
#if UNITY_EDITOR
                    Name = subScene.SceneAsset.name,
#endif
                    Scene = new EntitySceneReference(subScene.SceneGUID, 0),
                    TargetWorld = SubSceneLoadUtil.ConvertFlags(this.targetWorld),
                    LoadingMode = this.loadMode,
                    IsRequired = this.isRequired,
                    LoadMaxDistanceOverride = this.loadMaxDistanceOverride,
                    UnloadMaxDistanceOverride = this.unloadMaxDistanceOverride,
                };
            }
        }

        private void Awake()
        {
            this.GetComponent<SubScene>().AutoLoadScene = false;
        }

        private void OnValidate()
        {
            this.unloadMaxDistanceOverride = math.max(this.unloadMaxDistanceOverride, this.loadMaxDistanceOverride);
        }
    }
}
#endif
