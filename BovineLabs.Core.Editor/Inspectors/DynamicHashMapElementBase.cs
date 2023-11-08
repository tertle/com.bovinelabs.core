// // <copyright file="DynamicHashMapElementBase.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Core.Editor.Inspectors
// {
//     using System;
//     using BovineLabs.Core.Iterators;
//     using Unity.Entities;
//     using Unity.Entities.UI;
//
//     public abstract class DynamicHashMapElementBase<TBuffer, TKey, TValue> : DynamicBufferElementBase<TBuffer>
//         where TBuffer : unmanaged, IDynamicHashMap<TKey, TValue>
//         where TKey : unmanaged, IEquatable<TKey>
//         where TValue : unmanaged
//     {
//         protected DynamicHashMapElementBase(object inspector)
//             : base(inspector)
//         {
//         }
//
//         public DynamicHashMap<TKey, TValue> GetMap() =>
//             this.Context.EntityManager.GetBuffer<TBuffer>(this.Context.Entity).AsHashMap<TBuffer, TKey, TValue>();
//
//
//     }
// }
