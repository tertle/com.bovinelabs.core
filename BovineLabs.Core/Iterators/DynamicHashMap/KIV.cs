// <copyright file="KIV.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using System;
    using System.Diagnostics;
    using Unity.Collections.LowLevel.Unsafe;

    [DebuggerDisplay("Key = {Key}, Value = {Value}")]
    public unsafe struct KIV<TKey, TIndex, TValue>
        where TKey : unmanaged, IEquatable<TKey>
        where TIndex : unmanaged, IEquatable<TIndex>
        where TValue : unmanaged
    {
        internal DynamicIndexedMapHelper<TKey, TIndex, TValue>* Data;
        internal int Index;
        internal int Next;

        /// <summary> Gets an invalid KeyValue. </summary>
        public static KIV<TKey, TIndex, TValue> Null => new() { Index = -1 };

        /// <summary> Gets the key. </summary>
        /// <value> The key. If this KeyValue is Null, returns the default of TKey. </value>
        public TKey Key
        {
            get
            {
                if (this.Index != -1)
                {
                    return this.Data->KeyHash.Keys[this.Index];
                }

                return default;
            }
        }

        /// <summary> Gets the index. </summary>
        public TIndex Indexed
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
                if (this.Index == -1)
                {
                    throw new ArgumentException("must be valid");
                }
#endif

                return this.Data->IndexHash.Keys[this.Index];
            }
        }

        /// <summary> Gets the value. </summary>
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

                return ref UnsafeUtility.AsRef<TValue>(this.Data->Values + this.Index);
            }
        }
    }
}
