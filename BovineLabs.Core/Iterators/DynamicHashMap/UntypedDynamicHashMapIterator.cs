namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct UntypedDynamicHashMapIterator : IEnumerator<(IntPtr Key, IntPtr Value)>
    {
        private readonly int keySize;
        private readonly int valueSize;

        [NativeDisableUnsafePtrRestriction]
        private UntypedDynamicHashMapHelper.Enumerator enumerator;

        internal UntypedDynamicHashMapIterator(UntypedDynamicHashMapHelper* data, int keySize, int valueSize)
        {
            this.keySize = keySize;
            this.valueSize = valueSize;
            this.enumerator = new UntypedDynamicHashMapHelper.Enumerator(data);
        }

        public (IntPtr Key, IntPtr Value) Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var current = this.enumerator.GetCurrent();

                var helper = (UntypedDynamicHashMapHelper*)current.UntypedDynamicHashMapHelper;
                var key = helper->Keys + (this.keySize * current.Index);
                var value = helper->Values + (this.valueSize * current.Index);

                return ((IntPtr)key, (IntPtr)value);
            }
        }

        object IEnumerator.Current => this.Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return this.enumerator.MoveNext();
        }

        public void Reset()
        {
            this.enumerator.Reset();
        }

        public void Dispose()
        {
        }
    }
}
