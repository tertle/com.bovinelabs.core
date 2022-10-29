// <copyright file="BufferCapacityAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Entities;
    using Unity.Entities.Hybrid.Baking;
    using UnityEngine;

    /// <summary> Assembly attribute that can be used to override <see cref="InternalBufferCapacityAttribute"/> or default values. </summary>
    [AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple=true)]
    public class BufferCapacityAttribute : Attribute
    {
        public readonly Type Type;
        public readonly int Capacity;

        public BufferCapacityAttribute(Type type, int capacity = 0)
        {
            this.Type = type;
            this.Capacity = capacity;
        }
    }

#if UNITY_EDITOR
    // Editor initialization
    [UnityEditor.InitializeOnLoad]
#endif
    public static unsafe class ResizeBufferCapacity
    {
        static ResizeBufferCapacity()
        {
            InitializeAttributes();
        }

        // Runtime initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void InitializeAttributes()
        {
            foreach (var attr in ReflectionUtility.GetAllAssemblyAttributes<BufferCapacityAttribute>())
            {
                SetBufferCapacity(attr.Type, attr.Capacity);
            }
        }

        public static void SetBufferCapacity(Type type, int bufferCapacity)
        {
            TypeManager.Initialize();

            var index = TypeManager.GetTypeIndex(type).Index;
            var typeInfoPointer = TypeManager.GetTypeInfoPointer() + index;
            if (typeInfoPointer->Category != TypeManager.TypeCategory.BufferData)
            {
                Debug.LogError($"Trying to set buffer capacity on type ({type}) that isn't buffer");
                return;
            }

            *&typeInfoPointer->BufferCapacity = bufferCapacity;
            *&typeInfoPointer->SizeInChunk = sizeof(BufferHeader) + (bufferCapacity * typeInfoPointer->ElementSize);
        }

        public static void SetBufferCapacity<T>(int bufferCapacity)
                where T : unmanaged, IBufferElementData
        {
            TypeManager.Initialize();

            var index = TypeManager.GetTypeIndex<T>().Index;
            var typeInfoPointer = TypeManager.GetTypeInfoPointer() + index;
            *&typeInfoPointer->BufferCapacity = bufferCapacity;
            *&typeInfoPointer->SizeInChunk = sizeof(BufferHeader) + (bufferCapacity * typeInfoPointer->ElementSize);
        }
    }

    // Baking initialization
    [CreateBefore(typeof(LinkedEntityGroupBakingCleanUp))]
    [UpdateInGroup(typeof(PreBakingSystemGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public struct ResizeBufferSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
            ResizeBufferCapacity.InitializeAttributes();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
        }
    }
}
