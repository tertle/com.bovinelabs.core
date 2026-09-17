#if !UNITY_NETCODE
namespace Unity.Netcode
{
    using Unity.Collections;
    using Unity.Entities;

    public interface IInputComponentData : IComponentData
    {
        FixedString512Bytes ToFixedString()
        {
            return "?InputComponentData?";
        }
    }
}
#endif
