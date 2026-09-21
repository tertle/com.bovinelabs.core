namespace BovineLabs.Core.Collections
{
    using System;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Utility;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Mathematics;

    public readonly unsafe struct UnmanagedPool<T> : IDisposable
        where T : unmanaged
    {
        private readonly int _capacity;
        private readonly Allocator _allocator;

        [NativeDisableUnsafePtrRestriction]
        private readonly T* _buffer;

        [NativeDisableUnsafePtrRestriction]
        private readonly int* _length;

        [NativeDisableUnsafePtrRestriction]
        private readonly SpinLock* _spinner;

        public UnmanagedPool(int capacity, Allocator allocator = Allocator.Persistent)
        {
            capacity = GetCapacity(capacity);
            _capacity = capacity;
            _allocator = allocator;

            _buffer = (T*)UnsafeUtility.MallocTracked(sizeof(T) * capacity, UnsafeUtility.AlignOf<T>(), allocator, 0);
            _length = (int*)UnsafeUtility.MallocTracked(sizeof(int), UnsafeUtility.AlignOf<int>(), allocator, 0);
            *_length = 0;
            _spinner = (SpinLock*)UnsafeUtility.MallocTracked(sizeof(SpinLock), UnsafeUtility.AlignOf<SpinLock>(), allocator, 0);
            *_spinner = default;
        }

        public bool IsCreated => _buffer != null;

        public void Dispose()
        {
            UnsafeUtility.FreeTracked(_buffer, _allocator);
            UnsafeUtility.FreeTracked(_length, _allocator);
            UnsafeUtility.FreeTracked(_spinner, _allocator);
        }

        public bool TryAdd(T element)
        {
            _spinner->Acquire();

            if (*_length < _capacity)
            {
                _buffer[*_length] = element;
                *_length += 1;
                _spinner->Release();
                return true;
            }

            _spinner->Release();
            return false;
        }

        public bool TryGet(out T element)
        {
            _spinner->Acquire();

            if (*_length > 0)
            {
                *_length -= 1;
                element = _buffer[*_length];
                _spinner->Release();
                return true;
            }

            element = default;
            _spinner->Release();
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetCapacity(int newCapacity)
        {
            newCapacity = math.max(newCapacity, CollectionHelper.CacheLineSize / sizeof(T));
            newCapacity = math.ceilpow2(newCapacity);
            return newCapacity;
        }
    }
}
