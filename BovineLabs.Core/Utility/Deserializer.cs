namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct Deserializer
    {
        [ReadOnly]
        private NativeArray<byte> _data;

        public Deserializer(NativeArray<byte> data, int offset = 0)
        {
            _data = data;
            CurrentIndex = offset;
        }

        public Deserializer(byte* ptr, int length, int offset = 0)
        {
            _data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(ptr, length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref _data, AtomicSafetyHandle.Create());
#endif
            CurrentIndex = offset;
        }

        public bool IsCreated => _data.IsCreated;

        public NativeArray<byte> Data => _data;

        public int CurrentIndex { get; set; }

        public byte* Current => (byte*)_data.GetUnsafeReadOnlyPtr() + CurrentIndex;

        /// <summary>
        /// True only at the exact end; an index beyond the data is invalid and returns false.
        /// </summary>
        public bool IsAtEnd => CurrentIndex == _data.Length;

        public void Reset()
        {
            CurrentIndex = 0;
        }

        public T Peek<T>()
            where T : unmanaged
        {
            var ptr = (byte*)_data.GetUnsafeReadOnlyPtr() + CurrentIndex;
            var result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
            return result;
        }

        public T Peek<T>(int offset)
            where T : unmanaged
        {
            var ptr = (byte*)_data.GetUnsafeReadOnlyPtr() + CurrentIndex + offset;
            var result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
            return result;
        }

        public T Read<T>()
            where T : unmanaged
        {
            var ptr = (byte*)_data.GetUnsafeReadOnlyPtr() + CurrentIndex;

            var result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
            CurrentIndex += UnsafeUtility.SizeOf<T>();
            return result;
        }

        public T* ReadBuffer<T>(int length)
            where T : unmanaged
        {
            var ptr = (T*)((byte*)_data.GetUnsafeReadOnlyPtr() + CurrentIndex);
            CurrentIndex += length * UnsafeUtility.SizeOf<T>();
            return ptr;
        }

        public void Offset(int size)
        {
            CurrentIndex += size;
        }

        public void Offset<T>()
            where T : unmanaged
        {
            CurrentIndex += UnsafeUtility.SizeOf<T>();
        }
    }
}
