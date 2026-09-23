namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;
    using Unity.Collections.LowLevel.Unsafe;

    [NativeContainer]
    [NativeContainerIsReadOnly]
    public unsafe struct UntypedDynamicHashMapIterator : IEnumerator<Tu<IntPtr, IntPtr>>
    {
        private readonly int _keySize;
        private readonly int _valueSize;

        [NativeDisableUnsafePtrRestriction]
        private UntypedDynamicHashMapHelper.Enumerator _enumerator;

        internal UntypedDynamicHashMapIterator(UntypedDynamicHashMapHelper* data, int keySize, int valueSize)
        {
            _keySize = keySize;
            _valueSize = valueSize;
            _enumerator = new UntypedDynamicHashMapHelper.Enumerator(data);
        }

        public Tu<IntPtr, IntPtr> Current
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                var (helperPtr, index) = _enumerator.GetCurrent();

                var helper = (UntypedDynamicHashMapHelper*)helperPtr;
                var key = helper->Keys + (_keySize * index);
                var value = helper->Values + (_valueSize * index);

                return new Tu<IntPtr, IntPtr>((IntPtr)key, (IntPtr)value);
            }
        }

        object IEnumerator.Current => Current;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

        public void Dispose()
        {
        }
    }
}
