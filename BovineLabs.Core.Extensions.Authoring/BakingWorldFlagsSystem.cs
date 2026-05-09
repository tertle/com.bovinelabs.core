// <copyright file="BakingWorldFlagsSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;
    using Unity.NetCode.Hybrid;

    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct BakingWorldFlagsSystem : ISystem
    {
        /// <inheritdoc/>
        public void OnCreate(ref SystemState state)
        {
            var bakingSystem = state.World.GetExistingSystemManaged<BakingSystem>();
            var settings = bakingSystem.BakingSettings.DotsSettings;

            switch (settings)
            {
                case NetCodeServerSettings:
                    state.WorldUnmanaged.GetImpl().Flags |= WorldFlags.GameClient;
                    break;
                case NetCodeClientSettings:
                    state.WorldUnmanaged.GetImpl().Flags |= WorldFlags.GameServer;
                    break;
                case NetCodeClientAndServerSettings:
                    state.WorldUnmanaged.GetImpl().Flags |= WorldFlags.GameClient | WorldFlags.GameClient;
                    break;
            }
        }
    }
}
#endif