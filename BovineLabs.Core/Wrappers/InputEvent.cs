#if !UNITY_NETCODE
namespace Unity.Netcode
{
    using Unity.Collections;

    public struct InputEvent
    {
        public readonly bool IsSet => Count > 0;

        public uint Count;

        public void Set()
        {
            Count++;
        }

        public readonly FixedString32Bytes ToFixedString()
        {
            return $"InputEvent[{Count}]";
        }
    }
}
#endif
