// <copyright file="GhostDynamicHashMapAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using Unity.NetCode;

    /// <summary>
    /// This attribute can be used to tag dynamic hash map marker buffers to generate default NetCode ghost serializers.
    /// </summary>
    /// <remarks>
    /// GhostDynamicHashMap is not enough to make your buffer replicated. Make sure the buffer is included on a ghost prefab.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Struct, AllowMultiple = false)]
    public sealed class GhostDynamicHashMapAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the dynamic hash map value encoding mode used by the generated ghost serializer.
        /// </summary>
        public GhostDynamicHashMapCodecMode CodecMode { get; set; } = GhostDynamicHashMapCodecMode.Generated;

        /// <summary>
        /// Gets or sets the type of prefab where this component should be included on the main entity of the prefab.
        /// </summary>
        public GhostPrefabType PrefabType { get; set; } = GhostPrefabType.All;

        /// <summary>
        /// Gets or sets the type of ghost this component should be sent to if the ghost is owner predicted.
        /// Formerly: "OwnerPredictedSendType".
        /// </summary>
        public GhostSendType SendTypeOptimization { get; set; } = GhostSendType.AllClients;

        /// <summary>
        /// Get or sets if a component should be be sent to the prediction owner or not. Some combination
        /// of the parameters and OwnerSendType may result in an error or warning at code-generation time.
        /// </summary>
        public SendToOwnerType OwnerSendType { get; set; } = SendToOwnerType.All;

        /// <summary>
        /// Denotes whether or not this component - when added to a child entity - should send (i.e. replicate) its data.
        /// The default behaviour is that Netcode will NOT replicate component and buffer data on children.
        /// Why not? It's expensive, as it involves finding child entities in other chunks.
        /// Thus, setting this flag to true will enable this (more expensive) serialization of child entities (unless overridden via another "Variant").
        /// Setting to false has no effect (as is the default).
        /// </summary>
        public bool SendDataForChildEntity { get; set; }
    }
}
