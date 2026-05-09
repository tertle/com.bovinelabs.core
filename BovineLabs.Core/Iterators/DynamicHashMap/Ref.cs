// A proof of concept safer ref
// // <copyright file="Ref.cs" company="BovineLabs">
// //     Copyright (c) BovineLabs. All rights reserved.
// // </copyright>
//
// namespace BovineLabs.Core.Iterators
// {
//     using System;
//     using BovineLabs.Core.Extensions;
//     using Unity.Collections.LowLevel.Unsafe;
//     using Unity.Entities;
//
//     public unsafe readonly ref struct Ref<T>
//         where T : unmanaged
//     {
// #if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
//         private readonly DynamicBuffer<byte> buffer;
//         private readonly void* bufferPointer;
// #endif
//         private readonly byte* data;
//
// #if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
//         public Ref(DynamicBuffer<byte> buffer, byte* ptr)
// #else
//         public Ref(byte* ptr)
// #endif
//         {
//             this.data = ptr;
// #if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
//             this.buffer = buffer;
//             this.bufferPointer = buffer.GetPtr();
// #endif
//         }
//
//         public bool IsValid => this.data != null;
//
//         public T Value
//         {
//             get
//             {
// #if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
//                 this.RefCheck();
// #endif
//                 return UnsafeUtility.AsRef<T>(this.data);
//             }
//
//             set
//             {
// #if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
//                 this.RefCheck();
// #endif
//                 UnsafeUtility.AsRef<T>(this.data) = value;
//             }
//         }
//
//         // Using this is safe, but holding onto this value is unsafe as if you resize your underlying container, there will be no check for it
//         public ref T RefUnsafe
//         {
//             get
//             {
// #if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
//                 this.RefCheck();
// #endif
//                 return ref UnsafeUtility.AsRef<T>(this.data);
//             }
//         }
//
//         // public static implicit operator T(Ref<T> r)
//         // {
//         //     return r.Value;
//         // }
//
// #if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
//         private void RefCheck()
//         {
//             var ptr = this.buffer.GetPtr();
//             if (this.bufferPointer != ptr)
//             {
//                 throw new ArgumentException("Ref has been invalidated");
//             }
//         }
// #endif
//     }
// }
