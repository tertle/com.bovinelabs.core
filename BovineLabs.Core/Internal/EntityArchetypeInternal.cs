// <copyright file="EntityArchetypeInternal.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Internal
{
    using Unity.Entities;

    public static unsafe class EntityArchetypeInternal
    {
        public static int ArchetypeChunkInstanceSize(this EntityArchetype entityArchetype)
        {
            return entityArchetype.Archetype->InstanceSize;
        }

        public static int ArchetypeChunkInstanceSizeWithOverhead(this EntityArchetype entityArchetype)
        {
            return entityArchetype.Archetype->InstanceSizeWithOverhead;
        }

        public static int ArchetypeChunkNonZeroSizedTypesCount(this EntityArchetype entityArchetype)
        {
            return entityArchetype.Archetype->NonZeroSizedTypesCount;
        }

        public static int ArchetypeChunkTpeCount(this EntityArchetype entityArchetype)
        {
            return entityArchetype.Archetype->TypesCount;
        }

        public static TypeIndex ArchetypeChunkGetTypeIndex(this EntityArchetype entityArchetype, int index)
        {
            return entityArchetype.Archetype->Types[index].TypeIndex;
        }

        public static ushort ArchetypeChunkGetSize(this EntityArchetype entityArchetype, int index)
        {
            return entityArchetype.Archetype->SizeOfs[index];
        }
    }
}
