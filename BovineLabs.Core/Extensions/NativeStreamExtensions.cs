namespace BovineLabs.Core.Extensions
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Extensions for NativeEventStream. </summary>
    public static unsafe class NativeStreamExtensions
    {
        private static readonly int MaxSize = (4 * 1024) - sizeof(void*);

        /// <summary> Allocate a chunk of memory that can be larger than the max allocation size. </summary>
        /// <param name="writer"> The writer. </param>
        /// <param name="data"> The data to write. </param>
        /// <param name="size"> The size of the data. For an array, this is UnsafeUtility.SizeOf{T} * length. </param>
        public static void AllocateLarge(ref NativeStream.Writer writer, byte* data, int size)
        {
            if (size == 0)
            {
                return;
            }

            var allocationCount = size / MaxSize;
            var allocationRemainder = size % MaxSize;

            for (var i = 0; i < allocationCount; i++)
            {
                var ptr = writer.Allocate(MaxSize);
                UnsafeUtility.MemCpy(ptr, data + (i * MaxSize), MaxSize);
            }

            if (allocationRemainder > 0)
            {
                var ptr = writer.Allocate(allocationRemainder);
                UnsafeUtility.MemCpy(ptr, data + (allocationCount * MaxSize), allocationRemainder);
            }
        }

        /// <summary> Read a chunk of memory that could have been larger than the max allocation size. </summary>
        /// <param name="reader"> The reader. </param>
        /// <param name="size"> For an array, this is UnsafeUtility.SizeOf{T} * length. </param>
        /// <param name="allocator"> Allocator to use. </param>
        /// <returns> Pointer to data. </returns>
        public static byte* ReadLarge(ref NativeStream.Reader reader, int size, Allocator allocator = Allocator.Temp)
        {
            if (size == 0)
            {
                return default;
            }

            if (size < MaxSize)
            {
                return reader.ReadUnsafePtr(size);
            }

            var output = (byte*)UnsafeUtility.Malloc(size, 4, allocator);

            var allocationCount = size / MaxSize;
            var allocationRemainder = size % MaxSize;

            for (var i = 0; i < allocationCount; i++)
            {
                var ptr = reader.ReadUnsafePtr(MaxSize);
                UnsafeUtility.MemCpy(output + (i * MaxSize), ptr, MaxSize);
            }

            if (allocationRemainder > 0)
            {
                var ptr = reader.ReadUnsafePtr(allocationRemainder);
                UnsafeUtility.MemCpy(output + (allocationCount * MaxSize), ptr, allocationRemainder);
            }

            return output;
        }
    }
}