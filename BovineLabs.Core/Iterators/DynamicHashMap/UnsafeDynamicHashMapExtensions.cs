// <copyright file="UnsafeDynamicHashMapExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Iterators
{
    using BovineLabs.Core.Collections;
    using Unity.Assertions;

    public static unsafe class UnsafeDynamicHashMapExtensions
    {
        public static UntypedDynamicHashMapIterator GetUntypedIterator(UnsafeUntypedDynamicBuffer buffer, int keySize, int valueSize)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            Assert.AreEqual(1, buffer.ElementSize);
#endif
            var helper = (UntypedDynamicHashMapHelper*)buffer[0];
            return new UntypedDynamicHashMapIterator(helper, keySize, valueSize);
        }
    }
}
