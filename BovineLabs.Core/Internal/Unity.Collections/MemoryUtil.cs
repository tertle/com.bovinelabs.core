namespace BovineLabs.Core.Internal
{
    using Unity.Collections;

    public static class MemoryUtil
    {
        public static MemoryLabel CreateLabel(FixedString32Bytes category, FixedString64Bytes name, Allocator allocator = Allocator.Persistent)
        {
            return Unity.Collections.Memory.CreateLabel(category, name, allocator);
        }
    }
}
