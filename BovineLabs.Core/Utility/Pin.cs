// <copyright file="Pin.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System.Runtime.InteropServices;
    using JetBrains.Annotations;

    // Idea from https://stackoverflow.com/a/70917852
    public static class Pin
    {
        /// <summary>
        /// Use to obtain raw access to a managed object allowing pinning.
        /// Usage:<code>fixed (byte* data = &amp;GetRawObjectData(managed)){  }</code>
        /// </summary>
        /// <param name="o"> The object to get the raw value from. </param>
        /// <returns> The ref of the object. </returns>
        public static ref byte GetRawObjectData(object o)
        {
            return ref new PinnableUnion(o).Pinnable.Data;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct PinnableUnion
        {
            [FieldOffset(0)]
            public object Object;

            [FieldOffset(0)]
            public Pinnable Pinnable;

            public PinnableUnion(object o)
            {
                // TODO can use this in coreclr update
                // System.Runtime.CompilerServices.Unsafe.SkipInit(out this);
                this = default;
                this.Object = o;
            }
        }

        [UsedImplicitly]
        private sealed class Pinnable
        {
            public byte Data;
        }
    }
}
