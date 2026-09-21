namespace BovineLabs.Core.Utility
{
    using System.Runtime.InteropServices;
    using JetBrains.Annotations;

    // Idea from https://stackoverflow.com/a/70917852
    public static class Pin
    {
        /// <summary>
        /// Pin raw managed data with fixed (byte* data = &amp;GetRawObjectData(managed)).
        /// </summary>
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
                Object = o;
            }
        }

        [UsedImplicitly]
        private sealed class Pinnable
        {
            public byte Data;
        }
    }
}
