// <copyright file="Deserializer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Deserializer that convert a byte array into usable data. </summary>
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

        /// <summary> Gets the raw serialized data. </summary>
        public NativeArray<byte> Data => this.data;

        /// <summary>
        /// Gets or sets the current index being deserialized.
        /// In general you should not handle this manually and instead let the deserializer do it.
        /// </summary>
        public int CurrentIndex { get; set; }

        /// <summary> Gets a pointer to the current byte the <see cref="CurrentIndex" /> is pointing at. </summary>
        public byte* Current => (byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex;

        /// <summary> Gets a value indicating whether the <see cref="CurrentIndex" /> is pointing to the end of the data. </summary>
        /// <remarks> Note if the index is greater than the data then this will return false as that should be an invalid state. </remarks>
        public bool IsAtEnd => this.CurrentIndex == this.data.Length;

        /// <summary> Resets <see cref="CurrentIndex" /> to the start if you wanted to deserialize multiple times. </summary>
        public void Reset()
        {
            this.CurrentIndex = 0;
        }

        /// <summary> Look at the value at the <see cref="CurrentIndex" /> but don't increment it. </summary>
        /// <typeparam name="T"> The type to reinterpret as. </typeparam>
        /// <returns> The value at the location. </returns>
        public T Peek<T>()
            where T : unmanaged
        {
            var ptr = (byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex;
            var result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
            return result;
        }

        /// <summary> Look at the value at the <see cref="CurrentIndex" /> offset by <see cref="Offset{T}" /> but don't increment it. </summary>
        /// <param name="offset"> How much to offset the <see cref="CurrentIndex" />. </param>
        /// <typeparam name="T"> The type to reinterpret as. </typeparam>
        /// <returns> The value at the location. </returns>
        public T Peek<T>(int offset)
            where T : unmanaged
        {
            var ptr = (byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex + offset;
            var result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
            return result;
        }

        /// <summary> Read the value at the <see cref="CurrentIndex" /> and then increment the <see cref="CurrentIndex" /> by sizeof(T). </summary>
        /// <typeparam name="T"> The type to reinterpret as. </typeparam>
        /// <returns> The value at the location. </returns>
        public T Read<T>()
            where T : unmanaged
        {
            var ptr = (byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex;

            var result = UnsafeUtility.ReadArrayElement<T>(ptr, 0);
            this.CurrentIndex += UnsafeUtility.SizeOf<T>();
            return result;
        }

        /// <summary>
        /// Read <see cref="length" /> elements starting at the value at the <see cref="CurrentIndex" />
        /// and then increment the <see cref="CurrentIndex" /> by length * sizeof(T).
        /// </summary>
        /// <param name="length"> The number of elements to read. </param>
        /// <typeparam name="T"> The type to reinterpret as. </typeparam>
        /// <returns> The value at the location. </returns>
        public T* ReadBuffer<T>(int length)
            where T : unmanaged
        {
            var ptr = (T*)((byte*)this.data.GetUnsafeReadOnlyPtr() + this.CurrentIndex);
            this.CurrentIndex += length * UnsafeUtility.SizeOf<T>();
            return ptr;
        }

        /// <summary> Increment the <see cref="CurrentIndex" /> by <see cref="size" />. </summary>
        /// <param name="size"> The value to increment <see cref="CurrentIndex" /> by. </param>
        public void Offset(int size)
        {
            this.CurrentIndex += size;
        }

        /// <summary> Increment the <see cref="CurrentIndex" /> by sizeof(T). </summary>
        /// <typeparam name="T"> The type to get the size of. </typeparam>
        public void Offset<T>()
            where T : unmanaged
        {
            this.CurrentIndex += UnsafeUtility.SizeOf<T>();
        }
    }
}
