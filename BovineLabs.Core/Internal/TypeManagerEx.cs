// <copyright file="TypeManagerEx.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public abstract unsafe class TypeManagerEx
    {
        public static FixedString128Bytes GetComponentDebugName(TypeIndex typeIndex)
        {
            var fs = new FixedString128Bytes();
            fs.Append(TypeManager.GetTypeInfo(typeIndex).DebugTypeName);
            return fs;
        }

        public static FixedString128Bytes GetSystemDebugName(SystemTypeIndex systemIndex)
        {
            var unsafeText = TypeManager.GetSystemNameInternal(systemIndex);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var ro = new NativeText.ReadOnly(unsafeText, AtomicSafetyHandle.GetTempMemoryHandle());
#else
            var ro = new NativeText.ReadOnly(unsafeText);
#endif

            var fs = new FixedString128Bytes();
            fs.Append(ro);
            return fs;
        }
    }
}
