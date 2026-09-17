namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DynamicUntypedHashMap<TKey>
        where TKey : unmanaged, IEquatable<TKey>
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicUntypedHashMapHelper<TKey>* helper;

        internal DynamicUntypedHashMap(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsUntypedHelper<TKey>();
        }

        public readonly bool IsCreated => this.buffer.IsCreated;

        public readonly bool IsEmpty
        {
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return !this.IsCreated || this.helper->IsEmpty;
            }
        }

        public readonly int Count
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.helper->Count;
            }
        }

        /// <summary>
        /// Capacity cannot shrink.
        /// </summary>
        public int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                this.buffer.CheckReadAccess();
                this.RefCheck();
                return this.helper->Capacity;
            }

            set
            {
                this.buffer.CheckWriteAccess();
                this.RefCheck();
                DynamicUntypedHashMapHelper<TKey>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicUntypedHashMapHelper<TKey>* Helper => this.helper;

        public void Add<TValue>(TKey key, TValue item)
            where TValue : unmanaged
        {
            DynamicUntypedHashMapHelper<TKey>.AddUnique(this.buffer, ref this.helper, key, item);
        }

        public void AddOrSet<TValue>(TKey key, TValue item)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicUntypedHashMapHelper<TKey>.AddOrSet(this.buffer, ref this.helper, key, item);
        }

        public void AddOrSet(TKey key, void* value, int length)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicUntypedHashMapHelper<TKey>.AddOrSetRaw(this.buffer, ref this.helper, key, value, length);
        }

        /// <summary>
        /// The returned reference aliases map storage. Consume immediately; any later map write or capacity change invalidates it.
        /// </summary>
        public ref TValue GetOrAddRefUnsafe<TValue>(TKey key, TValue defaultValue = default)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicUntypedHashMapHelper<TKey>.AddUnique(this.buffer, ref this.helper, key, defaultValue);
            }

            return ref DynamicUntypedHashMapHelper<TKey>.GetValue<TValue>(this.helper, idx);
        }

        public byte* GetOrAddRaw(TKey key, void* defaultValue, int length, out int storedLength)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            if (idx == -1)
            {
                idx = DynamicUntypedHashMapHelper<TKey>.AddUniqueRaw(this.buffer, ref this.helper, key, defaultValue, length);
            }

            return DynamicUntypedHashMapHelper<TKey>.GetValueRaw(this.helper, idx, out storedLength);
        }

        public readonly bool TryGetValue<TValue>(TKey key, out TValue item)
            where TValue : unmanaged
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValue(key, out item);
        }

        public readonly bool TryGetValue(TKey key, out byte* value, out int length)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->TryGetValueRaw(key, out value, out length);
        }

        public readonly bool ContainsKey(TKey key)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();

            var idx = this.helper->Find(key);
            return idx != -1;
        }

        public readonly bool Remove(TKey key)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->TryRemove(key) != -1;
        }

        public readonly NativeArray<TKey> GetKeyArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetKeyArray(allocator);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            if (this.helper != this.buffer.GetPtr())
            {
                throw new ArgumentException("DynamicUntypedHashMap was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicUntypedHashMapHelper<TKey>>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }
    }
}
