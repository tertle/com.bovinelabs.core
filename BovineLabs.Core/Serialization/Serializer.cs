// <copyright file="Serializer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Serialization
{
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Entities.Serialization;

    /// <summary> Serialization and deserialization methods for Worlds. </summary>
    public static class Serializer
    {
        /// <summary> Serialize a world into a save file. </summary>
        /// <param name="world"> The world to serialize. </param>
        /// <returns> The save file. </returns>
        public static unsafe SaveFile Serialize(World world)
        {
            using (var serializeWorld = new World("Serialize"))
            {
                serializeWorld.EntityManager.CopyAndReplaceEntitiesFrom(world.EntityManager);

                using (var writer = new MemoryBinaryWriter())
                {
                    SerializeUtility.SerializeWorld(serializeWorld.EntityManager, writer, out var referencedObjects);

                    var data = new byte[writer.Length];
                    fixed (byte* ptr = data)
                    {
                        UnsafeUtility.MemCpy(ptr, writer.Data, writer.Length);
                    }

                    return new SaveFile(data, referencedObjects);
                }
            }
        }

        /// <summary> Deserialize a save file onto a world. </summary>
        /// <param name="world"> The world to deserialize onto. Any existing entities will be removed. </param>
        /// <param name="save"> The save file. </param>
        public static unsafe void Deserialize(World world, SaveFile save)
        {
            using (var deserializeWorld = new World("Deserialize"))
            {
                fixed (byte* ptr = save.Data)
                {
                    using (var reader = new MemoryBinaryReader(ptr))
                    {
                        var transaction = deserializeWorld.EntityManager.BeginExclusiveEntityTransaction();
                        SerializeUtility.DeserializeWorld(transaction, reader, save.ReferencedObjects);
                        deserializeWorld.EntityManager.EndExclusiveEntityTransaction();
                    }
                }

                world.EntityManager.DestroyEntity(world.EntityManager.UniversalQuery);
                world.EntityManager.PrepareForDeserialize();
                world.EntityManager.CopyAndReplaceEntitiesFrom(deserializeWorld.EntityManager);
            }
        }
    }
}