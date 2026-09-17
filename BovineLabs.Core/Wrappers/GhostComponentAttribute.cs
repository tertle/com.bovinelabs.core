#if !UNITY_NETCODE
namespace Unity.Netcode
{
    using System;

    [Flags]
    public enum GhostPrefabType
    {
        None = 0,
        InterpolatedClient = 1,
        PredictedClient = 2,
        Client = 3,
        Server = 4,
        AllPredicted = 6,
        All = 7
    }

    /// <summary>
    /// Send filtering applies to owner-predicted ghosts or fixed supported modes, not ghosts whose mode can switch at runtime.
    /// </summary>
    [Flags]
    public enum GhostSendType
    {
        DontSend = 0,
        OnlyInterpolatedClients = 1,
        OnlyPredictedClients = 2,
        AllClients = 3
    }

    [Flags]
    public enum SendToOwnerType
    {
        None = 0,
        SendToOwner = 1,
        SendToNonOwner = 2,
        All = 3,
    }

    /// <summary>
    /// Controls prefab variants and send policy; replication still requires GhostField on each replicated field.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
    public class GhostComponentAttribute : Attribute
    {
        public GhostPrefabType PrefabType { get; set; } = GhostPrefabType.All;
        public GhostSendType SendTypeOptimization { get; set; } = GhostSendType.AllClients;

        public SendToOwnerType OwnerSendType { get; set; } = SendToOwnerType.All;

        /// <summary>
        /// Child-entity replication is opt-in and may be overridden by a variant.
        /// </summary>
        public bool SendDataForChildEntity { get; set; } = false;
    }
}
#endif
