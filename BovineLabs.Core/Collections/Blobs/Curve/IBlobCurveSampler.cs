namespace BovineLabs.Core.Collections
{
    using System.Runtime.CompilerServices;

    public interface IBlobCurveSampler<out T>
        where T : unmanaged
    {
        bool IsCreated { get; }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T Evaluate(in float time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapMode(in float time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateWithoutCache(in float time);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EvaluateIgnoreWrapModeWithoutCache(in float time);
    }
}
