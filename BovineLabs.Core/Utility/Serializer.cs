// <copyright file="Serializer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> Serializer that convert usable data into a byte array. </summary>
    public unsafe struct Serializer : IDisposable
    {
        public Serializer(int capacity, Allocator allocator)
        {
            this.Data = UnsafeList<byte>.Create(capacity, allocator);
        }

        [field: NativeDisableUnsafePtrRestriction]
        public UnsafeList<byte>* Data { get; }

        public int Length => this.Data->Length;

        /// <summary> Ensures you can add this much extra capacity. The final capacity will be current capacity + capacity. </summary>
        /// <param name="capacity"> The additional capacity to add. </param>
        public void EnsureExtraCapacity(int capacity)
        {
            if (this.Data->Length + capacity > this.Data->Capacity)
            {
                this.Data->Capacity = this.Data->Length + capacity;
            }
        }

        public void Dispose()
        {
            UnsafeList<byte>.Destroy(this.Data);
        }

        public int AllocateNoResize<T>()
            where T : unmanaged
        {
            var idx = this.Data->Length;
            this.Data->m_length += UnsafeUtility.SizeOf<T>();
            return idx;
        }

        public int AllocateNoResize<T>(int length)
            where T : unmanaged
        {
            var idx = this.Data->Length;
            this.Data->m_length += length * UnsafeUtility.SizeOf<T>();
            return idx;
        }

        public int Allocate<T>()
            where T : unmanaged
        {
            var idx = this.Data->Length;
            this.Data->Resize(this.Data->Length + UnsafeUtility.SizeOf<T>());
            return idx;
        }

        public int Allocate<T>(int length)
            where T : unmanaged
        {
            var idx = this.Data->Length;
            this.Data->Resize(this.Data->Length + (length * UnsafeUtility.SizeOf<T>()));
            return idx;
        }

        public T* GetAllocation<T>(int idx)
            where T : unmanaged
        {
            return (T*)(this.Data->Ptr + idx);
        }

        public void AddNoResize<T>(T value)
            where T : unmanaged
        {
            this.Data->AddRangeNoResize(&value, UnsafeUtility.SizeOf<T>());
        }

        public void Add<T>(T value)
            where T : unmanaged
        {
            this.Data->AddRange(&value, UnsafeUtility.SizeOf<T>());
        }

        public void AddBufferNoResize<T>(NativeArray<T> value)
            where T : unmanaged
        {
            this.Data->AddRangeNoResize(value.GetUnsafeReadOnlyPtr(), value.Length * UnsafeUtility.SizeOf<T>());
        }

        public void AddBufferNoResize<T>(NativeSlice<T> value)
            where T : unmanaged
        {
            this.Data->ReserveNoResize(value.Length * UnsafeUtility.SizeOf<T>(), out var ptr, out _);
            var num = UnsafeUtility.SizeOf<T>();
            UnsafeUtility.MemCpyStride(ptr, num, value.GetUnsafeReadOnlyPtr(), value.Stride, num, value.Length);
        }

        public void AddBufferNoResize<T>(T* value, int length)
            where T : unmanaged
        {
            this.Data->AddRangeNoResize(value, length * UnsafeUtility.SizeOf<T>());
        }

        public void AddBuffer<T>(NativeArray<T> value)
            where T : unmanaged
        {
            this.Data->AddRange(value.GetUnsafeReadOnlyPtr(), value.Length * UnsafeUtility.SizeOf<T>());
        }

        public void AddBuffer<T>(NativeSlice<T> value)
            where T : unmanaged
        {
            var idx = this.Data->Length;
            this.Data->Resize(idx + (value.Length * UnsafeUtility.SizeOf<T>()));
            var ptr = this.Data->Ptr + idx;
            var num = UnsafeUtility.SizeOf<T>();
            UnsafeUtility.MemCpyStride(ptr, num, value.GetUnsafeReadOnlyPtr(), value.Stride, num, value.Length);
        }

        public void AddBuffer<T>(T* value, int length)
            where T : unmanaged
        {
            this.Data->AddRange(value, length * UnsafeUtility.SizeOf<T>());
        }

        public void AddBuffer(byte* ptr, int size)
        {
            this.Data->AddRange(ptr, size);
        }
    }
}
