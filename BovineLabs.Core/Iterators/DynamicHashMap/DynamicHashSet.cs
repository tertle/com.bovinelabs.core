namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    [DebuggerTypeProxy(typeof(DynamicHashSetDebuggerTypeProxy<>))]
    public unsafe struct DynamicHashSet<T> : IEnumerable<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicHashMapHelper<T>* helper;

        internal DynamicHashSet(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsHelper<T>();
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
                DynamicHashMapHelper<T>.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal readonly DynamicHashMapHelper<T>* Helper => this.helper;

        public readonly void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        public bool Add(T item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return DynamicHashMapHelper<T>.TryAdd(this.buffer, ref this.helper, item) != -1;
        }

        public readonly bool Remove(T item)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return this.helper->TryRemove(item) != -1;
        }

        public readonly bool Contains(T item)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->Find(item) != -1;
        }

        public void Flatten()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicHashMapHelper<T>.Flatten(this.buffer, ref this.helper);
        }

        public readonly NativeArray<T> ToNativeArray(AllocatorManager.AllocatorHandle allocator)
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return this.helper->GetKeyArray(allocator);
        }

        public readonly DynamicHashSetEnumerator<T> GetEnumerator()
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return new DynamicHashSetEnumerator<T>(this.helper);
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator<T> IEnumerable<T>.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Not implemented; use the concrete GetEnumerator instead.
        /// </summary>
        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            if (this.helper != this.buffer.GetPtr())
            {
                throw new ArgumentException("DynamicHashSet was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicHashMapHelper<T>>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }
    }

    internal sealed unsafe class DynamicHashSetDebuggerTypeProxy<T>
        where T : unmanaged, IEquatable<T>
    {
        private readonly DynamicHashMapHelper<T>* helper;

        public DynamicHashSetDebuggerTypeProxy(DynamicHashSet<T> target)
        {
            this.helper = target.Helper;
        }

        public List<T> Items
        {
            get
            {
                var result = new List<T>();

                if (this.helper == null)
                {
                    return result;
                }

                using var items = this.helper->GetKeyArray(Allocator.Temp);

                for (var i = 0; i < items.Length; ++i)
                {
                    result.Add(items[i]);
                }

                return result;
            }
        }
    }
}
