// <copyright file="TypeManagerEx.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using BovineLabs.Core.Extensions;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class TypeManagerEx
    {
        private static bool initialized;

        private static UnsafeList<FixedString128Bytes> typeNames;
        private static UnsafeList<FixedString128Bytes> systemTypeNames;

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
#else
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#endif
        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;

            TypeManager.Initialize();

            typeNames = new UnsafeList<FixedString128Bytes>(TypeManager.MaximumTypesCount, Allocator.Domain);
            foreach (var t in TypeManager.AllTypes)
            {
                var typeName = t.Type?.Name ?? "null";
                typeNames.Add(typeName);
            }

            SharedTypeNames.Ref.Data = new IntPtr(typeNames.Ptr);

            systemTypeNames = new UnsafeList<FixedString128Bytes>(1024, Allocator.Domain);
            var allSystemTypes = (List<Type>)typeof(TypeManager).GetField("s_SystemTypes", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null);

            foreach (var t in allSystemTypes)
            {
                var typeName = t?.Name ?? "null";
                systemTypeNames.Add(typeName.ToFixedString128NoError());
            }

            SharedSystemTypeNames.Ref.Data = new IntPtr(systemTypeNames.Ptr);
        }

        public static FixedString128Bytes GetTypeName(TypeIndex typeIndex)
        {
            return GetTypeNamesPointer()[typeIndex.Index];
        }

        public static FixedString128Bytes GetSystemName(SystemTypeIndex systemIndex)
        {
            return GetSystemTypeNamesPointer()[systemIndex.Index];
        }

        private static FixedString128Bytes* GetTypeNamesPointer()
        {
            return (FixedString128Bytes*)SharedTypeNames.Ref.Data;
        }

        private static FixedString128Bytes* GetSystemTypeNamesPointer()
        {
            return (FixedString128Bytes*)SharedSystemTypeNames.Ref.Data;
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
    }
}
