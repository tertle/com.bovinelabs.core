// <copyright file="GhostComponentAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_NETCODE
namespace Unity.NetCode
{
    using System;

    [Flags]
    public enum GhostPrefabType
    {
        InterpolatedClient = 1,
        PredictedClient = 2,
        Client = 3,
        Server = 4,
        AllPredicted = 6,
        All = 7
    }

    [Flags]
    public enum GhostSendType
    {
        Interpolated = 1,
        Predicted = 2,
        All = 3
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
    /// This attribute can be used to tag components to control which ghost prefab variants they are included in and where they are sent for owner predicted ghosts.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
    public class GhostComponentAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the type of prefab where this component should be included on the main entity of the prefab.
        /// </summary>
        public GhostPrefabType PrefabType {get; set;}
        /// <summary>
        /// Gets or sets the type of ghost this component should be sent to if the ghost is owner predicted.
        /// </summary>
        public GhostSendType OwnerPredictedSendType {get; set;}
        /// <summary>
        /// Get or sets to witch if a component should be be sent to the prediction owner or not. Some combination
        /// of the parameters and OwnerSendType may result in an error or warning at code-generation time.
        /// </summary>
        public SendToOwnerType OwnerSendType { get; set; }
        /// <summary>
        /// Gets or sets if the component should send data when it is on a child entity rather than the main entity of a prefab.
        /// </summary>
        public bool SendDataForChildEntity {get; set;}
        public GhostComponentAttribute()
        {
            PrefabType = GhostPrefabType.All;
            OwnerPredictedSendType = GhostSendType.All;
            OwnerSendType = SendToOwnerType.All;
            SendDataForChildEntity = true;
        }
    }
}
#endif
