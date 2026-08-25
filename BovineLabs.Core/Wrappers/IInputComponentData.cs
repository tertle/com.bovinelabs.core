// <copyright file="IInputComponentData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_NETCODE
namespace Unity.NetCode
{
    using Unity.Collections;
    using Unity.Entities;

    /// <summary>Stub for NetCode input components when com.unity.netcode is not installed.</summary>
    public interface IInputComponentData : IComponentData
    {
        /// <summary>Gets a fixed-string representation of the input component.</summary>
        FixedString512Bytes ToFixedString()
        {
            return "?InputComponentData?";
        }
    }
}
#endif
