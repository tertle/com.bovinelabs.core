#if !UNITY_NETCODE
namespace Unity.Netcode
{
    using Unity.Collections;

    public struct InputEvent
    {
        public readonly bool IsSet => this.Count > 0;

        public uint Count;

        public void Set()
        {
            this.Count++;
        }

        public readonly FixedString32Bytes ToFixedString()
        {
            return $"InputEvent[{this.Count}]";
        }
    }
}
#endif
