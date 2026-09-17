namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using System.Runtime.CompilerServices;
    using BovineLabs.Core.Extensions;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public unsafe struct DynamicUntypedBuffer
    {
        private readonly DynamicBuffer<byte> buffer;

        [NativeDisableUnsafePtrRestriction]
        private DynamicUntypedBufferHelper* helper;

        internal DynamicUntypedBuffer(DynamicBuffer<byte> buffer)
        {
            CheckSize(buffer);

            this.buffer = buffer;
            this.helper = buffer.AsUntypedBufferHelper();
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

        public readonly int Length
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
                DynamicUntypedBufferHelper.Resize(this.buffer, ref this.helper, value);
            }
        }

        internal DynamicUntypedBufferHelper* Helper => this.helper;

        public void Clear()
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            this.helper->Clear();
        }

        public int Add<TValue>(TValue value)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return DynamicUntypedBufferHelper.Add(this.buffer, ref this.helper, value);
        }

        public ref TValue ElementAt<TValue>(int index)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            return ref DynamicUntypedBufferHelper.GetValue<TValue>(this.helper, index);
        }

        public ref readonly TValue ElementAtRO<TValue>(int index)
            where TValue : unmanaged
        {
            this.buffer.CheckReadAccess();
            this.RefCheck();
            return ref DynamicUntypedBufferHelper.GetValue<TValue>(this.helper, index);
        }

        public void Set<TValue>(int index, TValue value)
            where TValue : unmanaged
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicUntypedBufferHelper.SetValue(this.helper, index, value);
        }

        public void RemoveAt(int index)
        {
            this.buffer.CheckWriteAccess();
            this.RefCheck();
            DynamicUntypedBufferHelper.RemoveAt(ref this.helper, index);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private readonly void RefCheck()
        {
            if (this.helper != this.buffer.GetPtr())
            {
                throw new ArgumentException("DynamicUntypedBuffer was not passed by ref when doing a resize and is now invalid");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        private static void CheckSize(DynamicBuffer<byte> buffer)
        {
            if (buffer.Length == 0)
            {
                throw new InvalidOperationException("Buffer not initialized");
            }

            if (buffer.Length < UnsafeUtility.SizeOf<DynamicUntypedBufferHelper>())
            {
                throw new InvalidOperationException("Buffer has data but is too small to be a header.");
            }
        }
    }
}
