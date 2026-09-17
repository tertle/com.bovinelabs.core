namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    public unsafe struct Deserializer
    {
        [ReadOnly]
        private NativeArray<byte> data;

        public Deserializer(NativeArray<byte> data, int offset = 0)
        {
            this.data = data;
            this.CurrentIndex = offset;
        }

        public Deserializer(byte* ptr, int length, int offset = 0)
        {
            this.data = NativeArrayUnsafeUtility.ConvertExistingDataToNativeArray<byte>(ptr, length, Allocator.None);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeArrayUnsafeUtility.SetAtomicSafetyHandle(ref this.data, AtomicSafetyHandle.Create());
#endif
            this.CurrentIndex = offset;
        }

        public bool IsCreated => this.data.IsCreated;

        public NativeArray<byte> Data => this.data;

        public int CurrentIndex { get; set; }

        public byte* Current => (byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex;

        /// <summary>
        /// True only at the exact end; an index beyond the data is invalid and returns false.
        /// </summary>
        public bool IsAtEnd => this.CurrentIndex == this.data.Length;

        public void Reset()
        {
            this.CurrentIndex = 0;
        }

        public T Peek<T>()
            where T : unmanaged
        {
            var ptr = (byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex;
            var result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
            return result;
        }

        public T Peek<T>(int offset)
            where T : unmanaged
        {
            var ptr = (byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex + offset;
            var result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
            return result;
        }

        public T Read<T>()
            where T : unmanaged
        {
            var ptr = (byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex;

            var result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
            this.CurrentIndex += UnsafeUtility.SizeOf<T>();
            return result;
        }

        public T* ReadBuffer<T>(int length)
            where T : unmanaged
        {
            var ptr = (T*)((byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex);
            this.CurrentIndex += length * UnsafeUtility.SizeOf<T>();
            return ptr;
        }

        public void Offset(int size)
        {
            this.CurrentIndex += size;
        }

        public void Offset<T>()
            where T : unmanaged
        {
            this.CurrentIndex += UnsafeUtility.SizeOf<T>();
        }
    }
}
