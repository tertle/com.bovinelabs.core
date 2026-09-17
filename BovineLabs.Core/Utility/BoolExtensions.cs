namespace BovineLabs.Core.Utility
{
    using System.Runtime.InteropServices;

    public static class BoolExtensions
    {
        public static byte AsByte(this bool value)
        {
            return new BoolUnion { Condition = value }.Value;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct BoolUnion
        {
            [FieldOffset(0)]
            public bool Condition;

            [FieldOffset(0)]
            public readonly byte Value;
        }
    }
}
