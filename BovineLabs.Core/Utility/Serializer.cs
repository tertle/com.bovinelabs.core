// <copyright file="Serializer.cs" company="BovineLabs">
// Copyright (c) BovineLabs. All rights reserved.
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
        private NativeList<byte> data;

        public Serializer(int capacity, Allocator allocator)
        {
            this.data = new NativeList<byte>(capacity, allocator);
        }

        public NativeList<byte> Data => this.data;

        public int Length => this.data.Length;

        /// <summary> Ensures you can add this much extra capacity. The final capacity will be current capacity + capacity. </summary>
        /// <param name="capacity"> The additional capacity to add. </param>
        public void EnsureExtraCapacity(int capacity)
        {
            if (this.data.Length + capacity > this.data.Capacity)
            {
                this.data.Capacity = this.data.Length + capacity;
            }
        }

        public void Dispose()
        {
            this.data.Dispose();
        }

        public int AllocateNoResize<T>()
            where T : unmanaged
        {
            var idx = this.data.Length;
            this.data.GetUnsafeList()->m_length += UnsafeUtility.SizeOf<T>();
            return idx;
        }

        public int AllocateNoResize<T>(int length)
            where T : unmanaged
        {
            var idx = this.data.Length;
            this.data.GetUnsafeList()->m_length += length * UnsafeUtility.SizeOf<T>();
            return idx;
        }

        public int Allocate<T>()
            where T : unmanaged
        {
            var idx = this.data.Length;
            this.data.ResizeUninitialized(this.data.Length + UnsafeUtility.SizeOf<T>());
            return idx;
        }

        public int Allocate<T>(int length)
            where T : unmanaged
        {
            var idx = this.data.Length;
            this.data.ResizeUninitialized(this.data.Length + length * UnsafeUtility.SizeOf<T>());
            return idx;
        }

        public T* GetAllocation<T>(int idx)
            where T : unmanaged
        {
            return (T*)((byte*)this.data.GetUnsafePtr() + idx);
        }

        public void AddNoResize<T>(T value)
            where T : unmanaged
        {
            this.data.AddRangeNoResize(&value, UnsafeUtility.SizeOf<T>());
        }

        public void Add<T>(T value)
            where T : unmanaged
        {
            this.data.AddRange(&value, UnsafeUtility.SizeOf<T>());
        }

        public void AddBufferNoResize<T>(NativeArray<T> value)
            where T : unmanaged
        {
            this.data.AddRangeNoResize(value.GetUnsafeReadOnlyPtr(), value.Length * UnsafeUtility.SizeOf<T>());
        }

        public void AddBufferNoResize<T>(NativeSlice<T> value)
            where T : unmanaged
        {
            this.data.ReserveNoResize(value.Length * UnsafeUtility.SizeOf<T>(), out var ptr, out _);
            var num = UnsafeUtility.SizeOf<T>();
            UnsafeUtility.MemCpyStride(ptr, num, value.GetUnsafeReadOnlyPtr(), value.Stride, num, value.Length);
        }

        public void AddBufferNoResize<T>(T* value, int length)
            where T : unmanaged
        {
            this.data.AddRangeNoResize(value, length * UnsafeUtility.SizeOf<T>());
        }

        public void AddBuffer<T>(NativeArray<T> value)
            where T : unmanaged
        {
            this.data.AddRange(value.GetUnsafeReadOnlyPtr(), value.Length * UnsafeUtility.SizeOf<T>());
        }

        public void AddBuffer<T>(NativeSlice<T> value)
            where T : unmanaged
        {
            var idx = this.data.Length;
            this.data.ResizeUninitialized(idx + value.Length * UnsafeUtility.SizeOf<T>());
            var ptr = (byte*)this.data.GetUnsafePtr() + idx;
            var num = UnsafeUtility.SizeOf<T>();
            UnsafeUtility.MemCpyStride(ptr, num, value.GetUnsafeReadOnlyPtr(), value.Stride, num, value.Length);
        }

        public void AddBuffer<T>(T* value, int length)
            where T : unmanaged
        {
            this.data.AddRange(value, length * UnsafeUtility.SizeOf<T>());
        }

        public void AddBuffer(byte* ptr, int size)
        {
            this.data.AddRange(ptr, size);
        }
    }
}
