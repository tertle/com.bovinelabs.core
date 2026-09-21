namespace BovineLabs.Core.Utility
{
    using System.Runtime.InteropServices;
    using Unity.Mathematics;

    [StructLayout(LayoutKind.Explicit)]
    public struct ShortHalfUnion
    {
        [FieldOffset(0)]
        public short ShortValue;

        [FieldOffset(0)]
        public half HalfValue;

        public ShortHalfUnion(short value)
        {
            HalfValue = default;
            ShortValue = value;
        }

        public ShortHalfUnion(half value)
        {
            ShortValue = default;
            HalfValue = value;
        }
    }
}
