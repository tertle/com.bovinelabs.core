// <copyright file="CreateImportEntity.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_6000_6_OR_NEWER
namespace BovineLabs.Core.SubScenes
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Utility;
    using Unity.Burst;
    using Unity.Entities;

    public interface IImportEntitySetup
    {
        void Setup(EntityManager entityManager, Entity importEntity, Hash128 sceneGuid);
    }

    public static unsafe class CreateImportEntity
    {
        private static readonly List<IImportEntitySetup> Creators = new();

        public static void Initialize()
        {
            foreach (var p in ReflectionUtility.GetAllImplementations<IImportEntitySetup>())
            {
                Creators.Add((IImportEntitySetup)Activator.CreateInstance(p));
            }
        }

        public static void Execute(void* argumentsPtr, int argumentsSize)
        {
            ref var arguments = ref BurstTrampoline.ArgumentsFromPtr<BurstManagedTriple<EntityManager, Entity, Hash128>>(argumentsPtr, argumentsSize);
            var entityManager = arguments.First;
            var importEntity = arguments.Second;
            var sceneGuid = arguments.Third;

            foreach (var c in Creators)
            {
                c.Setup(entityManager, importEntity, sceneGuid);
            }
        }
    }
}
#endif
