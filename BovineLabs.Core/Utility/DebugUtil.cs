namespace BovineLabs.Core.Utility
{
    using BovineLabs.Core.Assertions;
    using Unity.Mathematics;

    public static class DebugUtil
    {
        public static void SplitInt(float value, int digits, out int integer, out int decimals)
        {
            Check.Assume(digits >= 0);

            var multi = math.pow(10, digits);

            integer = (int)math.trunc(value);
            decimals = (int)(math.frac(value) * multi);
        }

        public static void SplitInt(double value, int digits, out int integer, out int decimals)
        {
            Check.Assume(digits >= 0);

            var multi = math.pow(10, digits);

            integer = (int)math.trunc(value);
            decimals = (int)(math.frac(value) * multi);
        }
    }
}
