// <copyright file="SubSceneLoadConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
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
        [Tooltip("The distance value for loading a BoundingVolume.")]
        [SerializeField]
        private float loadMaxDistance = 128;

        [Min(0)]
        [Tooltip("The distance value for unloading a BoundingVolume.")]
        [SerializeField]
        private float unloadMaxDistance = 144;

        public bool IsValid
        {
            get
            {
                var subScene = this.GetComponent<SubScene>();
                return subScene != null && subScene.SceneGUID != default;
            }
        }

        public SubSceneLoad GetSubSceneLoad()
        {
            var subScene = this.GetComponent<SubScene>();
            if (subScene == null || subScene.SceneGUID == default)
            {
                return default;
            }

            return new SubSceneLoad
            {
#if UNITY_EDITOR
                Name = subScene.SceneAsset.name,
#endif
                Scene = new EntitySceneReference(subScene.SceneGUID, 0),
                TargetWorld = SubSceneLoadUtil.ConvertFlags(this.targetWorld),
                LoadingMode = this.loadMode,
                IsRequired = this.isRequired,
                LoadMaxDistance = this.loadMaxDistance,
                UnloadMaxDistance = this.unloadMaxDistance,
            };
        }

        private void Awake()
        {
            this.GetComponent<SubScene>().AutoLoadScene = false;
        }

        private void OnValidate()
        {
            this.unloadMaxDistance = math.max(this.unloadMaxDistance, this.loadMaxDistance);
        }
    }
}
#endif
