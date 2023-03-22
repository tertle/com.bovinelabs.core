// <copyright file="SubSceneLoadMode.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Scenes;

    /// <summary> Specifies how a <see cref="SubScene" /> should be loaded and unloaded. </summary>
    public enum SubSceneLoadMode : byte
    {
        /// <summary> Automatically loads the <see cref="SubScene" /> at the start of the game. </summary>
        AutoLoad,

        /// <summary>
        /// Automatically load and unload the <see cref="SubScene" /> when the <see cref="Player" />
        /// comes within the <see cref="GameConfig.LoadMaxDistance" />.
        /// </summary>
        BoundingVolume,

        /// <summary>
        /// Manually load and unload the <see cref="SubScene" />.
        /// </summary>
        OnDemand,
    }
}
#endif
