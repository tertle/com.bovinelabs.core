// <copyright file="GhostDynamicHashMapAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_NETCODE
namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.NetCode;

    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class GhostDynamicHashMapAttribute : Attribute
    {
        public GhostDynamicHashMapCodecMode CodecMode { get; set; } = GhostDynamicHashMapCodecMode.Generated;

        public bool IsDefault { get; set; }

        public string DisplayName { get; set; }

        public GhostPrefabType PrefabType { get; set; } = GhostPrefabType.All;

        public GhostSendType SendTypeOptimization { get; set; } = GhostSendType.AllClients;

        public SendToOwnerType OwnerSendType { get; set; } = SendToOwnerType.All;

        public bool SendDataForChildEntity { get; set; }
    }
}
#endif
