#if !UNITY_NETCODE
namespace Unity.NetCode
{
    using System;

    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public sealed class GhostEnabledBitAttribute : Attribute
    {
    }
}
#endif
