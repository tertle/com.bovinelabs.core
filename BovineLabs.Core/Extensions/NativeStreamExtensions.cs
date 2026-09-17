namespace BovineLabs.Core.Extensions
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public static unsafe class NativeStreamExtensions
    {
        private static readonly int MaxSize = 4096 - sizeof(void*);

        public static void WriteLarge<T>(this ref NativeStream.Writer writer, NativeArray<T> array)
            where T : unmanaged
        {
            var byteArray = array.Reinterpret<byte>(UnsafeUtility.SizeOf<T>());
            WriteLarge(ref writer, (byte*)byteArray.GetUnsafeReadOnlyPtr(), byteArray.Length);
        }

        public static void WriteLarge<T>(this ref NativeStream.Writer writer, NativeSlice<T> data)
            where T : unmanaged
        {
            var num = UnsafeUtility.SizeOf<T>();
            var countPerAllocate = MaxSize / num;

            var allocationCount = data.Length / countPerAllocate;
            var allocationRemainder = data.Length % countPerAllocate;

            var maxSize = countPerAllocate * num;
            var maxOffset = data.Stride * countPerAllocate;

            var src = (byte*)data.GetUnsafeReadOnlyPtr();

            // Write the remainder first as this helps avoid an extra allocation most of the time
            // as you'd usually write at minimum the length beforehand
            if (allocationRemainder > 0)
            {
                var dst = writer.Allocate(allocationRemainder * num);
                UnsafeUtility.MemCpyStride(dst, num, src + (allocationCount * maxOffset), data.Stride, num, allocationRemainder);
            }

            for (var i = 0; i < allocationCount; i++)
            {
                var dst = writer.Allocate(maxSize);
                UnsafeUtility.MemCpyStride(dst, num, src + (i * maxOffset), data.Stride, num, countPerAllocate);
            }
        }

        public static void WriteLarge(this ref NativeStream.Writer writer, byte* data, int size)
        {
            var allocationCount = size / MaxSize;
            var allocationRemainder = size % MaxSize;

            // Write the remainder first as this helps avoid an extra allocation most of the time
            // as you'd usually write at minimum the length beforehand
            if (allocationRemainder > 0)
            {
                var ptr = writer.Allocate(allocationRemainder);
                UnsafeUtility.MemCpy(ptr, data + (allocationCount * MaxSize), allocationRemainder);
            }

            for (var i = 0; i < allocationCount; i++)
            {
                var ptr = writer.Allocate(MaxSize);
                UnsafeUtility.MemCpy(ptr, data + (i * MaxSize), MaxSize);
            }
        }

        public static void ReadLarge(this ref NativeStream.Reader reader, byte* buffer, int size)
        {
            var allocationCount = size / MaxSize;
            var allocationRemainder = size % MaxSize;

            // Write the remainder first as this helps avoid an extra chunk allocation most times
            if (allocationRemainder > 0)
            {
                var ptr = reader.ReadUnsafePtr(allocationRemainder);
                UnsafeUtility.MemCpy(buffer + (allocationCount * MaxSize), ptr, allocationRemainder);
            }

            for (var i = 0; i < allocationCount; i++)
            {
                var ptr = reader.ReadUnsafePtr(MaxSize);
                UnsafeUtility.MemCpy(buffer + (i * MaxSize), ptr, MaxSize);
            }
        }
    }
}
