// <copyright file="AfterPreSpawnedGhostConversion.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using Unity.NetCode;

    /// <summary> PreSpawnedGhostsConversion is internal, this let's you update after it. </summary>
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    [UpdateAfter(typeof(PreSpawnedGhostsConversion))]
    public class AfterPreSpawnedGhostConversion : GameObjectConversionSystem
    {
        /// <inheritdoc/>
        protected override void OnUpdate()
        {
        }
    }
}
#endif
