// <copyright file="ResizeBufferCapacity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Collections;
    using Unity.Entities;
#if UNITY_EDITOR
    using UnityEditor;
#endif
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
                    Debug.LogWarning($"Trying to remap stable hash {s.StableHash} but could not find type index from TypeManager");
                    continue;
                }

                SetBufferCapacity(t.Index, s.Capacity);
            }

            // TODO might be needed in future
            // typeof(BindingRegistry).GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
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
