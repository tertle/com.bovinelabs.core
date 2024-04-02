// <copyright file="SubSceneLoadConfig.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Scenes;
    using UnityEngine;

    /// <summary> Marks a <see cref="SubScene" /> as only loading in a specific world type. </summary>
    [RequireComponent(typeof(SubScene))]
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-1)] // just needs to execute before SubScene to stop autoload
    public class SubSceneLoadConfig : MonoBehaviour
    {
        [SerializeField]
#if UNITY_NETCODE
        private SubSceneLoadFlags targetWorld = SubSceneLoadFlags.ThinClient | SubSceneLoadFlags.Client | SubSceneLoadFlags.Server;
#else
        private SubSceneLoadFlags targetWorld = SubSceneLoadFlags.Game;
#endif

        [SerializeField]
        private SubSceneLoadMode loadMode = SubSceneLoadMode.BoundingVolume;

        [SerializeField]
        private bool isRequired;

        [Min(0)]
        [SerializeField]
        private float loadMaxDistanceOverride;

        [Min(0)]
        [SerializeField]
        private float unloadMaxDistanceOverride;

        /// <summary> Gets the target world to load the <see cref="SubScene" /> into. </summary>
        public WorldFlags TargetWorld => this.ConvertFlags();

        /// <summary> Gets the loading mode to load the <see cref="SubScene" />. </summary>
        public SubSceneLoadMode LoadingMode => this.loadMode;

        /// <summary> Gets a value indicating whether asynchronous streaming should be disabled. </summary>
        public bool IsRequired => this.isRequired;

        /// <summary>
        /// Gets the distance value for loading a <see cref="SubSceneLoadMode.BoundingVolume" />.
        /// A value less than or equal to zero will fall back to the <see cref="GameConfig.LoadMaxDistance" />.
        /// </summary>
        public float LoadMaxDistanceOverride => this.loadMaxDistanceOverride;

        /// <summary>
        /// Gets the distance value for unloading a <see cref="SubSceneLoadMode.BoundingVolume" />.
        /// A value less than or equal to zero will fall back to the <see cref="GameConfig.UnloadMaxDistance" />.
        /// </summary>
        public float UnloadMaxDistanceOverride => this.unloadMaxDistanceOverride;

        private void Awake()
        {
            this.GetComponent<SubScene>().AutoLoadScene = false;
        }

        private void OnValidate()
        {
            this.unloadMaxDistanceOverride = math.max(this.unloadMaxDistanceOverride, this.loadMaxDistanceOverride);
        }

        private WorldFlags ConvertFlags()
        {
            var flags = WorldFlags.None;
#if UNITY_NETCODE
            if ((this.targetWorld & SubSceneLoadFlags.Client) != 0)
            {
                flags |= WorldFlags.GameClient;
            }

            if ((this.targetWorld & SubSceneLoadFlags.Server) != 0)
            {
                flags |= WorldFlags.GameServer;
            }

            if ((this.targetWorld & SubSceneLoadFlags.ThinClient) != 0)
            {
                flags |= WorldFlags.GameThinClient;
            }
#else
            if ((this.targetWorld & SubSceneLoadFlags.Game) != 0)
            {
                flags |= WorldFlags.Game;
            }
#endif
            if ((this.targetWorld & SubSceneLoadFlags.Service) != 0)
            {
                flags |= Worlds.ServiceWorld;
            }

            // Remove the live flag
            flags &= ~WorldFlags.Live;
            return flags;
        }
    }
}
#endif
