namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.Netcode;

    /// <summary>
    /// The attribute generates serializers but does not enable replication; include the buffer on a ghost prefab.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class GhostDynamicHashMapAttribute : Attribute
    {
        public GhostDynamicHashMapCodecMode CodecMode { get; set; } = GhostDynamicHashMapCodecMode.Generated;

        public GhostPrefabType PrefabType { get; set; } = GhostPrefabType.All;

        public GhostSendType SendTypeOptimization { get; set; } = GhostSendType.AllClients;

        public SendToOwnerType OwnerSendType { get; set; } = SendToOwnerType.All;

        /// <summary>
        /// Child-entity replication is opt-in and may be overridden by a variant.
        /// </summary>
        public bool SendDataForChildEntity { get; set; }
    }
}
