// <copyright file="TypeManagerEx.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using UnityEngine;

    public static unsafe class TypeManagerEx
    {
        private static bool initialized;
        private static bool appDomainUnloadRegistered;

        private static UnsafeList<UnsafeText> typeNames;
        private static UnsafeList<UnsafeText> systemTypeNames;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            TypeManager.Initialize();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            SharedSafetyHandle.Ref.Data = AtomicSafetyHandle.Create();
#endif

            typeNames = new UnsafeList<UnsafeText>(TypeManager.MaximumTypesCount, Allocator.Persistent);
            foreach (var t in TypeManager.AllTypes)
            {
                var typeName = t.Type?.Name ?? "null";
                var unsafeName = new UnsafeText(Encoding.UTF8.GetByteCount(typeName), Allocator.Persistent);
                unsafeName.CopyFrom(typeName);
                typeNames.Add(unsafeName);
            }

            SharedTypeNames.Ref.Data = new IntPtr(typeNames.Ptr);

            systemTypeNames = new UnsafeList<UnsafeText>(1024, Allocator.Persistent);
            var allSystemTypes = (List<Type>)typeof(TypeManager).GetField("s_SystemTypes", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

            foreach (var t in allSystemTypes)
            {
                var typeName = t?.Name ?? "null";
                var unsafeName = new UnsafeText(Encoding.UTF8.GetByteCount(typeName), Allocator.Persistent);
                unsafeName.CopyFrom(typeName);
                systemTypeNames.Add(unsafeName);
            }

            SharedSystemTypeNames.Ref.Data = new IntPtr(systemTypeNames.Ptr);

            if (!appDomainUnloadRegistered)
            {
                // important: this will always be called from a special unload thread (main thread will be blocking on this)
                AppDomain.CurrentDomain.DomainUnload += (_, __) => Shutdown();

                // There is no domain unload in player builds, so we must be sure to shut down when the process exits.
                AppDomain.CurrentDomain.ProcessExit += (_, __) => Shutdown();
                appDomainUnloadRegistered = true;
            }
        }

        public static FixedString128Bytes GetTypeName(TypeIndex typeIndex)
        {
            return new FixedString128Bytes(*GetTypeNameInternal(typeIndex));
        }

        public static NativeText.ReadOnly GetSystemName(SystemTypeIndex systemIndex)
        {
            var pUnsafeText = GetSystemNameInternal(systemIndex);
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            NativeText.ReadOnly ro = new NativeText.ReadOnly(pUnsafeText, SharedSafetyHandle.Ref.Data);
#else
            NativeText.ReadOnly ro = new NativeText.ReadOnly(pUnsafeText);
#endif
            return ro;
        }

        private static void Shutdown()
        {
            if (!initialized)
            {
                return;
            }

            initialized = false;

            foreach (var name in typeNames)
            {
                name.Dispose();
            }

            typeNames.Dispose();

            foreach (var name in systemTypeNames)
            {
                name.Dispose();
            }

            systemTypeNames.Dispose();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.Release(SharedSafetyHandle.Ref.Data);
#endif
        }

        private static UnsafeText* GetTypeNameInternal(TypeIndex typeIndex)
        {
            return GetTypeNamesPointer() + typeIndex.Index;
        }

        private static UnsafeText* GetSystemNameInternal(SystemTypeIndex systemIndex)
        {
            return GetSystemTypeNamesPointer() + systemIndex.Index;
        }

        private static UnsafeText* GetTypeNamesPointer()
        {
            return (UnsafeText*)SharedTypeNames.Ref.Data;
        }

        private static UnsafeText* GetSystemTypeNamesPointer()
        {
            return (UnsafeText*) SharedSystemTypeNames.Ref.Data;
        }

        private struct TypeManagerKeyContext
        {
        }

        private struct SharedTypeNames
        {
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedTypeNames>();
        }

        private struct SharedSystemTypeNames
        {
            public static readonly SharedStatic<IntPtr> Ref = SharedStatic<IntPtr>.GetOrCreate<TypeManagerKeyContext, SharedSystemTypeNames>();
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        private struct SharedSafetyHandle
        {
            public static readonly SharedStatic<AtomicSafetyHandle> Ref = SharedStatic<AtomicSafetyHandle>.GetOrCreate<TypeManagerKeyContext, AtomicSafetyHandle>();
        }
#endif
    }
}
