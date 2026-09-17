namespace BovineLabs.Core.Internal
{
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using Unity.Burst.CompilerServices;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    internal static class CollectionChecks
    {
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckAllocator(AllocatorManager.AllocatorHandle allocator) => CollectionHelper.CheckAllocator(allocator);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: AssumeRange(0, int.MaxValue)]
        internal static int AssumePositive(int value) => CollectionHelper.AssumePositive(value);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal static void InitNativeContainer<T>(AtomicSafetyHandle handle) => CollectionHelper.InitNativeContainer<T>(handle);
#endif

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_ENABLE_CHECKS")]
        internal static void CheckIndexInRange(int index, int length) => CollectionHelper.CheckIndexInRange(index, length);
    }
}
