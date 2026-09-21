namespace BovineLabs.Core.Utility
{
    using System.Runtime.InteropServices;

    [StructLayout(LayoutKind.Explicit)]
    public struct IntFloatUnion
    {
        [FieldOffset(0)]
        public int IntValue;

        [FieldOffset(0)]
        public float FloatValue;

        public IntFloatUnion(int value)
        {
            FloatValue = 0;
            IntValue = value;
        }

        public IntFloatUnion(float value)
        {
            IntValue = 0;
            FloatValue = value;
        }
    }
}
