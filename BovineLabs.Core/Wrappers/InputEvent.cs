// <copyright file="InputEvent.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !UNITY_NETCODE
namespace Unity.NetCode
{
    using Unity.Collections;

    /// <summary>Stub for NetCode one-shot input events when com.unity.netcode is not installed.</summary>
    public struct InputEvent
    {
        /// <summary>Gets a value indicating whether the event has been set.</summary>
        public readonly bool IsSet => this.Count > 0;

        /// <summary>Gets the number of times the event was set.</summary>
        public uint Count;

        /// <summary>Sets the event.</summary>
        public void Set()
        {
            this.Count++;
        }

        /// <summary>Gets a fixed-string representation of the event.</summary>
        public readonly FixedString32Bytes ToFixedString()
        {
            return $"InputEvent[{this.Count}]";
        }
    }
}
#endif
