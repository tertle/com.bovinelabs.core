// <copyright file="TypeMangerEx.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_ENABLE_LINKED_CHUNKS
namespace BovineLabs.Core.Chunks
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using Unity.Assertions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
#if UNITY_EDITOR
    using UnityEditor;
#endif
    using UnityEngine;

    public static unsafe class TypeMangerEx
    {
        private static NativeArray<byte> groupIndices;
        private static bool initialized;
        private static bool appDomainUnloadRegistered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            TypeManager.Initialize();

            var chunkMap = new Dictionary<string, byte>();

            var chunkSettings = Resources.Load<VirtualChunkSettings>("VirtualChunkSettings");
#if UNITY_EDITOR
            if (chunkSettings == null)
            {
                // this is required because bakers don't like resource loading sometimes
                // TODO configurable
                chunkSettings = AssetDatabase.LoadAssetAtPath<VirtualChunkSettings>("Assets/Settings/Resources/VirtualChunkSettings.asset");
            }
#endif

            if (chunkSettings != null)
            {
                foreach (var map in chunkSettings.Mappings)
                {
                    chunkMap.Add(map.Label.ToLower(), map.Chunk);
                }
            }

            initialized = true;

            try
            {
#if UNITY_EDITOR
                if (!UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                {
                    throw new InvalidOperationException("Must be called from the main thread");
                }
#endif

                if (!appDomainUnloadRegistered)
                {
                    // important: this will always be called from a special unload thread (main thread will be blocking on this)
                    AppDomain.CurrentDomain.DomainUnload += (_, _) =>
                    {
                        if (initialized)
                        {
                            Shutdown();
                        }
                    };

                    // There is no domain unload in player builds, so we must be sure to shutdown when the process exits.
                    AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                    {
                        Shutdown();
                    };

                    appDomainUnloadRegistered = true;
                }

                groupIndices = new NativeArray<byte>(TypeManager.GetTypeCount(), Allocator.Persistent);

                int index = -1;

                foreach (var typeInfo in TypeManager.AllTypes)
                {
                    index++;
                    if (typeInfo.Type == null)
                    {
                        continue;
                    }

                    byte group = 0;


                    var virtualChunkAttribute = typeInfo.Type.GetCustomAttribute<VirtualChunkAttribute>() ??
                                                typeInfo.Type.Assembly.GetCustomAttribute<VirtualChunkAttribute>();

                    if (virtualChunkAttribute is { Group: not null })
                    {
                        chunkMap.TryGetValue(virtualChunkAttribute.Group.ToLower(), out group);
                    }

                    Assert.IsTrue(group < ChunkLinks.MaxGroupIDs);
                    groupIndices[index] = group;
                }

                SharedGroupIndices.Ref.Data = new IntPtr(groupIndices.GetUnsafeReadOnlyPtr());
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                Shutdown();
            }
        }

        public static byte GetGroupIndex<T>()
        {
            var typeIndex = TypeManager.GetTypeIndex<T>().Index;
            return GetGroupIndexPointer()[typeIndex];
        }

        public static byte GetGroupIndex(ComponentType type)
        {
            return GetGroupIndexPointer()[type.TypeIndex.Index];
        }

        private static byte* GetGroupIndexPointer()
        {
            return (byte*) SharedGroupIndices.Ref.Data;
        }

        private static void Shutdown()
        {
            // TODO, with module loaded type info, we cannot shutdown
#if UNITY_EDITOR
            if (!UnityEditorInternal.InternalEditorUtility.CurrentThreadIsMainThread())
                throw new InvalidOperationException("Must be called from the main thread");
#endif

            if (!initialized)
                return;

            initialized = false;

            groupIndices.Dispose();
        }

        private struct TypeMangerExContext { }

        private struct SharedGroupIndices
        {
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeMangerExContext, SharedGroupIndices>();
        }
    }
}
#endif
