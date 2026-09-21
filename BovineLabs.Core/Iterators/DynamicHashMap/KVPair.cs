namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;

    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    public unsafe struct KVPair<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal DynamicHashMapHelper<TKey>* Data;
        internal int Index;
        internal int Next;

        public static KVPair<TKey, TValue> Null => new() { Index = -1 };

        /// <summary>
        /// Returns default(TKey) for a null KeyValue.
        /// </summary>
        public TKey Key
        {
            get
            {
                if (Index != -1)
                {
                    return Data->Keys[Index];
                }

                return default;
            }
        }

        public ref TValue Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (Index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return ref UnsafeUtility.AsRef<TValue>(Data->Values + (Data->SizeOfTValue * Index));
            }
        }

        public bool GetKeyValue(out TKey key, out TValue value)
        {
            if (Index != -1)
            {
                key = Data->Keys[Index];
                value = UnsafeUtility.ReadArrayElement<TValue>(Data->Values, Index);
                return true;
            }

            key = default;
            value = default;
            return false;
        }
    }
}
