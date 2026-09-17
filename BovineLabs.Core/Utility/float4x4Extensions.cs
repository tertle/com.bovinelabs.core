namespace BovineLabs.Core.Utility
{
    using Unity.Mathematics;

    public static class float4x4Extensions
    {
        public static float UniformScale(this float4x4 float4X4)
        {
            return math.length(float4X4.c0.xyz);
        }
    }
}
