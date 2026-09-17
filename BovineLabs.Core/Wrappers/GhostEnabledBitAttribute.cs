#if !UNITY_NETCODE
namespace Unity.NetCode
{
    using System;

    /// <summary> Marks an enableable component for enabled-state replication when NetCode is installed. </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class GhostEnabledBitAttribute : Attribute
    {
    }
}
#endif
