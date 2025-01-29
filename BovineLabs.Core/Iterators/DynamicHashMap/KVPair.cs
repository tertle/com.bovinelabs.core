// <copyright file="KVPair.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;

    /// <summary> A key-value pair. </summary>
    /// <remarks> Used for enumerators. </remarks>
    /// <typeparam name="TKey"> The type of the keys. </typeparam>
    /// <typeparam name="TValue"> The type of the values. </typeparam>
    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    [GenerateTestsForBurstCompatibility(GenericTypeArguments = new[] { typeof(int), typeof(int) })]
    public unsafe struct KVPair<TKey, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TValue : unmanaged
    {
        internal DynamicHashMapHelper<TKey>* Data;
        internal int Index;
        internal int Next;

        /// <summary> An invalid KeyValue. </summary>
        public static KVPair<TKey, TValue> Null => new() { Index = -1 };

        /// <summary>
        /// The key.
        /// </summary>
        /// <value> The key. If this KeyValue is Null, returns the default of TKey. </value>
        public TKey Key
        {
            get
            {
                if (this.Index != -1)
                {
                    return this.Data->Keys[this.Index];
                }

                return default;
            }
        }

        /// <summary>
        /// Value of key/value pair.
        /// </summary>
        public ref TValue Value
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (this.Index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return ref UnsafeUtility.AsRef<TValue>(this.Data->Values + (sizeof(TValue) * this.Index));
            }
        }

        /// <summary>
        /// Gets the key and the value.
        /// </summary>
        /// <param name="key"> Outputs the key. If this KeyValue is Null, outputs the default of TKey. </param>
        /// <param name="value"> Outputs the value. If this KeyValue is Null, outputs the default of TValue. </param>
        /// <returns> True if the key-value pair is valid. </returns>
        public bool GetKeyValue(out TKey key, out TValue value)
        {
            if (this.Index != -1)
            {
                key = this.Data->Keys[this.Index];
                value = UnsafeUtility.ReadArrayElement<TValue>(this.Data->Values, this.Index);
                return true;
            }

            key = default;
            value = default;
            return false;
        }
    }
}
