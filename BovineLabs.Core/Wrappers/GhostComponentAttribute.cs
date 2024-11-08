// <copyright file="GhostComponentAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_NETCODE
namespace Unity.NetCode
{
    using System;

    /// <summary>
    /// Assign to every <see cref="GhostInstance"/>, and denotes which Ghost prefab version this component is allowed to exist on.
    /// Use this to disable rendering components on the Server version of the Ghost.
    /// If you cannot change the ComponentType, use the `GhostAuthoringInspectionComponent` to manually override on a specific Ghost prefab.
    /// </summary>
    [Flags]
    public enum GhostPrefabType
    {
        /// <summary>Component will not be added to any Ghost prefab type.</summary>
        None = 0,
        /// <summary>Component will only be added to the <see cref="GhostMode.Interpolated"/> Client version.</summary>
        InterpolatedClient = 1,
        /// <summary>Component will only be added to the <see cref="GhostMode.Predicted"/> Client version.</summary>
        PredictedClient = 2,
        /// <summary>Component will only be added to Client versions.</summary>
        Client = 3,
        /// <summary>Component will only be added to the Server version.</summary>
        Server = 4,
        /// <summary>Component will only be added to the Server and PredictedClient versions.</summary>
        AllPredicted = 6,
        /// <summary>Component will be to all versions.</summary>
        All = 7
    }

    /// <summary>
    /// <para>An optimization: Set on each GhostComponent via the <see cref="GhostComponentAttribute"/> (or via a variant).</para>
    /// <para>When a Ghost is <see cref="GhostMode.OwnerPredicted"/>, OR its SupportedGhostModes is known at compile time,
    /// this flag will filter which types of clients will receive data updates.</para>
    /// <para>Maps to the <see cref="GhostMode"/> of each Ghost.</para>
    /// <para>Note that this optimization is <b>not</b> available to Ghosts that can have their <see cref="GhostMode"/>
    /// modified at runtime!</para>
    /// </summary>
    /// <remarks>
    /// <para>GhostSendType works for OwnerPredicted ghosts because:</para>
    /// <para>- The server <b>can</b> infer what GhostMode any given client will have an OwnerPredicted ghost in.
    /// It's as simple as: If Owner, then Predicting, otherwise Interpolating.</para>
    /// <para>- The server <b>cannot</b> infer what GhostMode a ghost supporting both Predicted and Interpolated can be in,
    /// as this can change at runtime (see <see cref="GhostPredictionSwitchingQueues"/>.
    /// Thus, the server snapshot serialization strategy must be identical for both.</para>
    /// <para>GhostSendType <i>also</i> works for Ghosts not using <see cref="GhostModeMask.All"/> because:</para>
    /// <para>- The server <b>can</b> infer what GhostMode any given client will have its ghost in, as it cannot change at runtime.</para>
    /// <para>Applies to all components (parents and children).</para>
    /// </remarks>
    /// <example>
    /// A velocity component may only be required on a client if the ghost is being predicted (to predict velocity and collisions correctly).
    /// Thus, use GhostSendType.Predicted on the Velocity component.
    /// </example>
    [Flags]
    public enum GhostSendType
    {
        /// <summary>The server will never replicate this component to any clients.
        /// Works similarly to <see cref="DontSerializeVariant"/> (and thus, redundant, if the DontSerializeVariant is in use).</summary>
        DontSend = 0,
        /// <summary>The server will only replicate this component to clients which are interpolating this Ghost. <see cref="GhostMode.Interpolated"/>).</summary>
        OnlyInterpolatedClients = 1,
        /// <summary>The server will only replicate this component to clients which are predicted this Ghost. <see cref="GhostMode.Predicted"/>).</summary>
        OnlyPredictedClients = 2,
        /// <summary>The server will always replicate this component. Default.</summary>
        AllClients = 3
    }

    /// <summary>
    /// <para><b>Meta-data of a <see cref="ICommandData"/> component, denoting whether or not the server should replicate the
    /// input commands back down to clients.
    /// Configure via <see cref="GhostComponentAttribute"/>.</b></para>
    /// <para>See the documentation for ICommandData:<see cref="ICommandData"/></para>
    /// </summary>
    [Flags]
    public enum SendToOwnerType
    {
        /// <summary>Informs the server not not replicate this <see cref="ICommandData"/> back down to any clients.</summary>
        None = 0,
        /// <summary>Informs the server to replicate this <see cref="ICommandData"/> back to the owner, exclusively.</summary>
        SendToOwner = 1,
        /// <summary>Informs the server to replicate this <see cref="ICommandData"/> to all clients except the input "author"
        /// (i.e. the player who owns the ghost).</summary>
        SendToNonOwner = 2,
        /// <summary>Informs the server to replicate this <see cref="ICommandData"/> to all clients, including back to ourselves.</summary>
        All = 3,
    }

    /// <summary>
    /// This attribute can be used to tag components to control which ghost prefab variants they are included in and where they are sent for owner predicted ghosts.
    /// </summary>
    /// <remarks>
    /// GhostComponent is not enough to make your component replicated. Make sure to use <see cref="GhostFieldAttribute"/> on each replicated field.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
    public class GhostComponentAttribute : Attribute
    {
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
        public bool SendDataForChildEntity { get; set; } = false;
    }
}
#endif
