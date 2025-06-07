// <copyright file="TypeManagerOverrides.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_TYPEMANAGER_OVERRIDE
namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Unity.Burst;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public static unsafe class TypeManagerOverrides
    {
        static TypeManagerOverrides()
        {
            Initialize();
        }

        // Runtime initialization
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void Initialize()
        {
            TypeManager.Initialize();

            var r = Resources.Load<TypeManagerOverrideSettings>("TypeManagerOverrideSettings");
            if (r == null)
            {
#if UNITY_EDITOR
                // this is required because bakers don't like resource loading
                // TODO configurable
                r = AssetDatabase.LoadAssetAtPath<TypeManagerOverrideSettings>("Assets/Settings/Resources/TypeManagerOverrideSettings.asset");
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
                    BLDebug.LogWarningString($"Trying to remap stable hash {s.StableHash} but could not find type index from TypeManager");
                    continue;
                }

                SetBufferCapacity(t.Index, s.Capacity);
            }

            foreach (var stableHash in r.Enableables)
            {
                SetEnableable(stableHash);
            }

            // TODO might be needed in future
            // typeof(BindingRegistry).GetMethod("Initialize", BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(null, null);
        }

        private static void SetBufferCapacity(int index, int bufferCapacity)
        {
            var typeInfoPointer = TypeManager.GetTypeInfoPointer() + index;
            if (typeInfoPointer->Category != TypeManager.TypeCategory.BufferData)
            {
                BLDebug.Error($"Trying to set buffer capacity on typeindex ({index}) that isn't buffer");
                return;
            }

            *&typeInfoPointer->BufferCapacity = bufferCapacity;
            *&typeInfoPointer->SizeInChunk = sizeof(BufferHeader) + (bufferCapacity * typeInfoPointer->ElementSize);
        }

        private static void SetEnableable(ulong stableHash)
        {
            var typeIndex = TypeManager.GetTypeIndexFromStableTypeHash(stableHash);
            if (typeIndex == default)
            {
                BLDebug.LogWarningString($"Trying to make {stableHash} IEnableable but could not find type index from TypeManager");
                return;
            }

            var typeInfoPointer = TypeManager.GetTypeInfoPointer() + typeIndex.Index;
            if (typeInfoPointer->Category != TypeManager.TypeCategory.BufferData && typeInfoPointer->Category != TypeManager.TypeCategory.ComponentData)
            {
                BLDebug.Error($"Trying to set buffer capacity on typeindex ({typeIndex.Index}) that isn't buffer");
                return;
            }

            var type = typeInfoPointer->Type;
            var typeManagerKeyContext =
                Type.GetType("Unity.Entities.TypeManager+TypeManagerKeyContext, Unity.Entities, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");

            typeIndex.Value |= TypeManager.EnableableComponentFlag;
            *&typeInfoPointer->TypeIndex = typeIndex;

            SharedStatic<TypeIndex>.GetOrCreate(typeManagerKeyContext, type).Data = typeIndex;

            var managedTypeToIndex = (Dictionary<Type, TypeIndex>)typeof(TypeManager)
                .GetField("s_ManagedTypeToIndex", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

            var stableTypeHashToTypeIndex = (UnsafeParallelHashMap<ulong, TypeIndex>)typeof(TypeManager)
                .GetField("s_StableTypeHashToTypeIndex", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

            managedTypeToIndex[type] = typeIndex;
            stableTypeHashToTypeIndex[stableHash] = typeIndex;
        }
    }
}
#endif
