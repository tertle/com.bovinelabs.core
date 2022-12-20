// <copyright file="ResizeBufferCapacity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Entities;
    using UnityEditor;
    using UnityEngine;

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static unsafe class ResizeBufferCapacity
    {
        static ResizeBufferCapacity()
        {
            Initialize();
        }

        // Runtime initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Initialize()
        {
            TypeManager.Initialize();

            var r = Resources.Load<BufferCapacitySettings>("BufferCapacitySettings");
            if (r == null)
            {
#if UNITY_EDITOR
                // this is required because bakers don't like resource loading
                // TODO configurable
                r = AssetDatabase.LoadAssetAtPath<BufferCapacitySettings>("Assets/Configs/Settings/Resources/BufferCapacitySettings.asset");
                if (r == null)
#endif
                {
                    return;
                }
            }

            foreach (var s in r.BufferCapacities)
            {
                var t = TypeManager.GetTypeIndexFromStableTypeHash(s.StableHash);
                if (t == default)
                {
                    continue;
                }

                var type = TypeManager.GetType(t);
                SetBufferCapacity(type, s.Capacity);
            }

            foreach (var attr in ReflectionUtility.GetAllAssemblyAttributes<BufferCapacityAttribute>())
            {
                SetBufferCapacity(attr.Type, attr.Capacity);
            }
        }

        public static void SetBufferCapacity(Type type, int bufferCapacity)
        {
            TypeManager.Initialize();
            SetBufferCapacity(TypeManager.GetTypeIndex(type).Index, bufferCapacity);
        }

        public static void SetBufferCapacity<T>(int bufferCapacity)
            where T : unmanaged, IBufferElementData
        {
            TypeManager.Initialize();
            SetBufferCapacity(TypeManager.GetTypeIndex<T>().Index, bufferCapacity);
        }

        private static void SetBufferCapacity(int index, int bufferCapacity)
        {
            var typeInfoPointer = TypeManager.GetTypeInfoPointer() + index;
            if (typeInfoPointer->Category != TypeManager.TypeCategory.BufferData)
            {
                Debug.LogError($"Trying to set buffer capacity on typeindex ({index}) that isn't buffer");
                return;
            }

            *&typeInfoPointer->BufferCapacity = bufferCapacity;
            *&typeInfoPointer->SizeInChunk = sizeof(BufferHeader) + (bufferCapacity * typeInfoPointer->ElementSize);
        }
    }
}
