// // <copyright file="UnsafeDynamicHashMapExtensions.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Core.Iterators
// {
//     using System;
//
//     public static unsafe class UnsafeDynamicHashMapExtensions
//     {
//         public static bool FindFirst<TKey, TValue>(this DynamicMultiHashMap<TKey, TValue> hashMap, TKey key, TValue value, out TValue* result)
//             where TKey : unmanaged, IEquatable<TKey>
//             where TValue : unmanaged, IEquatable<TValue>
//         {
//             return DynamicHashMapBase<TKey, TValue>.FindFirst(hashMap.BufferReadOnly, key, value, out result);
//         }
//
//         public static bool Contains<TKey, TValue>(this DynamicMultiHashMap<TKey, TValue> hashMap, TKey key, TValue value)
//             where TKey : unmanaged, IEquatable<TKey>
//             where TValue : unmanaged, IEquatable<TValue>
//         {
//             return DynamicHashMapBase<TKey, TValue>.FindFirst(hashMap.BufferReadOnly, key, value, out _);
//         }
//     }
// }
