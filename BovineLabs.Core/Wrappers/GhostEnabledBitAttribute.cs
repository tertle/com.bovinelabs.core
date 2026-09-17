#if !UNITY_NETCODE
namespace Unity.Netcode
{
    using System;

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class GhostEnabledBitAttribute : Attribute
    {
    }
}
#endif
