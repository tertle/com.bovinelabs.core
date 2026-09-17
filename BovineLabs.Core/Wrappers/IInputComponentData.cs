#if !UNITY_NETCODE
namespace Unity.NetCode
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
