namespace BovineLabs.Core.Extensions
{
    using System.Runtime.CompilerServices;
    using Unity.Mathematics;
    using MinMaxAABB = Unity.Mathematics.MinMaxAABB;

    public static class MinMaxAABBExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Overlaps(this MinMaxAABB a, MinMaxAABB b)
        {
            return math.all((a.Max >= b.Min) & (a.Min <= b.Max));
        }
    }
}
