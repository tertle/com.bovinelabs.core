namespace BovineLabs.Core.Extensions
{
    using System;
    using System.Diagnostics;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Iterators;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;

    public static unsafe class EntityManagerExtensions
    {
        private const EntityQueryOptions QueryOptions = EntityQueryOptions.IncludeSystems;

        public static Entity GetEntityFromIndex(this EntityManager entityManager, int index)
        {
            return index is < 0 or >= EntityComponentStore.EntityStore.MaximumTheoreticalAmountOfEntities
                ? Entity.Null
                : entityManager.GetEntityByEntityIndex(index);
        }

        public static int NumberOfArchetype(this EntityManager entityManager)
        {
            return entityManager.GetCheckedEntityDataAccess()->EntityComponentStore->m_Archetypes.Length;
        }

        // Only use these for tests
        public static ComponentLookup<T> GetComponentLookup<T>(this EntityManager entityManager, bool isReadOnly = false)
            where T : unmanaged, IComponentData
        {
            return entityManager.GetComponentLookup<T>(isReadOnly);
        }

        // Only use these for tests
        public static BufferLookup<T> GetBufferLookup<T>(this EntityManager entityManager, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            return entityManager.GetBufferLookup<T>(isReadOnly);
        }

        public static UntypedDynamicBuffer GetUntypedBuffer(
            this EntityManager entityManager, Entity entity, ComponentType componentType, bool isReadOnly = false)
        {
            var access = entityManager.GetCheckedEntityDataAccess();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var safetyHandles = &access->DependencyManager->Safety;
#endif

            var typeIndex = componentType.TypeIndex;

            return access->GetUntypedBuffer(componentType, entity,
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                safetyHandles->GetSafetyHandle(typeIndex, isReadOnly), safetyHandles->GetBufferSafetyHandle(typeIndex),
#endif
                isReadOnly);
        }

        public static bool HasSingleton<T>(this EntityManager em)
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

            return query.CalculateEntityCount() == 1;
        }

        public static T GetSingleton<T>(this EntityManager em, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingleton<T>();
        }

        public static RefRW<T> GetSingletonRW<T>(this EntityManager em, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAllRW<T>().WithOptions(QueryOptions).Build(em);
            if (completeDependency)
            {
                query.CompleteDependency();
            }

            return query.GetSingletonRW<T>();
        }

        public static bool TryGetSingleton<T>(this EntityManager em, out T component, bool completeDependency = true)
            where T : unmanaged, IComponentData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

            if (query.CalculateEntityCount() != 1)
            {
                component = default;

                return false;
            }

            if (completeDependency)
            {
                query.CompleteDependency();
            }

            component = query.GetSingleton<T>();

            return true;
        }

        public static DynamicBuffer<T> GetSingletonBuffer<T>(this EntityManager em, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);

            return query.GetSingletonBuffer<T>(isReadOnly);
        }

        public static DynamicBuffer<T> GetSingletonBufferNoSync<T>(this EntityManager em, bool isReadOnly = false)
            where T : unmanaged, IBufferElementData
        {
            using var query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().WithOptions(QueryOptions).Build(em);
            return query.GetSingletonBufferNoSync<T>(isReadOnly);
        }

        public static Entity CreateEntity<T>(this EntityManager em, T component, FixedString64Bytes name)
            where T : unmanaged, IComponentData
        {
            var e = em.CreateEntity<T>(name);
            em.SetComponentData(e, component);
            return e;
        }

        public static Entity CreateEntity<T>(this EntityManager em, FixedString64Bytes name)
            where T : unmanaged
        {
            Span<ComponentType> s = stackalloc ComponentType[1];
            s[0] = ComponentType.ReadWrite<T>();

            var e = em.CreateEntity(s);
            em.SetName(e, name);
            return e;
        }

        public static Entity CreateEntity<T1, T2>(this EntityManager em, FixedString64Bytes name)
            where T1 : unmanaged
            where T2 : unmanaged
        {
            Span<ComponentType> s = stackalloc ComponentType[2];
            s[0] = ComponentType.ReadWrite<T1>();
            s[1] = ComponentType.ReadWrite<T2>();

            var e = em.CreateEntity(s);
            em.SetName(e, name);
            return e;
        }

        /// <summary>
        /// Instantiates <paramref name="srcEntity"/> as <see cref="EntityManager.Instantiate(Entity)"/> does, with <paramref name="sharedComponent"/>
        /// as the value of <typeparamref name="T"/> on every entity of the clone that has <typeparamref name="T"/>, its
        /// <see cref="LinkedEntityGroup"/> included. The clone is created in the chunks of that value, so no chunk move follows, as one would after
        /// <see cref="EntityManager.SetSharedComponent{T}(Entity, T)"/>. Entities without <typeparamref name="T"/> are instantiated unchanged.
        /// </summary>
        /// <exception cref="ArgumentException">In checks builds, when neither <paramref name="srcEntity"/> nor any entity of its
        /// <see cref="LinkedEntityGroup"/> has <typeparamref name="T"/>.</exception>
        public static Entity Instantiate<T>(this EntityManager entityManager, Entity srcEntity, T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            Entity entity;
            InstantiateWithSharedComponent(entityManager, srcEntity, &entity, 1, sharedComponent);
            return entity;
        }

        /// <summary>
        /// Makes <c>outputEntities.Length</c> clones of <paramref name="srcEntity"/> as
        /// <see cref="EntityManager.Instantiate(Entity, NativeArray{Entity})"/> does, with <paramref name="sharedComponent"/> as the value of
        /// <typeparamref name="T"/> on every entity of each clone that has <typeparamref name="T"/>, its <see cref="LinkedEntityGroup"/> included.
        /// Clones are created in the chunks of that value, so no chunk move follows. Entities without <typeparamref name="T"/> are instantiated
        /// unchanged. To give clones different values, call once per value.
        /// </summary>
        /// <exception cref="ArgumentException">In checks builds, when neither <paramref name="srcEntity"/> nor any entity of its
        /// <see cref="LinkedEntityGroup"/> has <typeparamref name="T"/>.</exception>
        public static void Instantiate<T>(this EntityManager entityManager, Entity srcEntity, NativeArray<Entity> outputEntities, T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            InstantiateWithSharedComponent(entityManager, srcEntity, (Entity*)outputEntities.GetUnsafePtr(), outputEntities.Length, sharedComponent);
        }

        private static void InstantiateWithSharedComponent<T>(EntityManager entityManager, Entity srcEntity, Entity* outputEntities, int count,
            T sharedComponent)
            where T : unmanaged, ISharedComponentData
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var entityComponentStore = access->EntityComponentStore;
            var archetype = entityComponentStore->GetArchetype(srcEntity);
            access->PrepareForAdditiveStructuralChanges(archetype->InstantiateArchetype);

            entityComponentStore->AssertEntitiesExist(&srcEntity, 1);
            entityComponentStore->AssertCanInstantiateEntities(srcEntity);
            AssertGroupHasSharedComponent<T>(entityComponentStore, srcEntity);

            var changes = access->BeginAdditiveStructuralChanges();

            // The reference this insert takes is released by EndStructuralChanges; each chunk created for the value takes its own.
            var sharedComponentIndex = access->InsertSharedComponent_Unmanaged(sharedComponent);

            if (StructuralChangesProfiler.Enabled)
            {
                access->StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in access->m_WorldUnmanaged);
            }

            EntityComponentStoreInstantiate.InstantiateWithSharedComponent(
                entityComponentStore, &srcEntity, outputEntities, count, TypeManager.GetTypeIndex<T>(), sharedComponentIndex);

            if (StructuralChangesProfiler.Enabled)
            {
                access->StructuralChangesRecorder.End();
            }

            access->EndStructuralChanges(ref changes);
        }

        /// <summary>
        /// Instantiates each group's prefab into reserved root entities, in one structural change: group <c>i</c> fills
        /// <c>entities[Start .. Start + Count]</c>, ids from <see cref="EntityReservation"/> that have not been filled. With
        /// <see cref="InstantiateGroup{T}.SetValue"/>, the group's clones are created in the chunks of its <see cref="InstantiateGroup{T}.Value"/>
        /// as <see cref="Instantiate{T}(EntityManager, Entity, NativeArray{Entity}, T)"/> does; without, they keep the prefab's values. Child
        /// entities of a <see cref="LinkedEntityGroup"/> are created, as only roots are reserved.
        /// </summary>
        /// <exception cref="ArgumentException">In checks builds, when a group's range leaves <paramref name="entities"/>, or a group setting its
        /// value has no entity with <typeparamref name="T"/>.</exception>
        public static void InstantiateExisting<T>(this EntityManager entityManager, NativeArray<Entity> entities, NativeArray<InstantiateGroup<T>> groups)
            where T : unmanaged, ISharedComponentData
        {
            if (groups.Length == 0)
            {
                return;
            }

            var access = entityManager.GetCheckedEntityDataAccess();
            var entityComponentStore = access->EntityComponentStore;
            foreach (var group in groups)
            {
                CheckGroupRange(group.Start, group.Count, entities.Length);
                var archetype = entityComponentStore->GetArchetype(group.Prefab);
                access->PrepareForAdditiveStructuralChanges(archetype->InstantiateArchetype);
                entityComponentStore->AssertCanInstantiateEntities(group.Prefab);
                if (group.SetValue)
                {
                    AssertGroupHasSharedComponent<T>(entityComponentStore, group.Prefab);
                }
            }

            var changes = access->BeginAdditiveStructuralChanges();

            var typeIndex = TypeManager.GetTypeIndex<T>();
            var existing = new NativeArray<EntityComponentStoreInstantiate.ExistingGroup>(
                groups.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (var i = 0; i < groups.Length; i++)
            {
                var group = groups[i];

                // Each insert's reference is released by EndStructuralChanges; each chunk created for the value takes its own.
                existing[i] = new EntityComponentStoreInstantiate.ExistingGroup
                {
                    Prefab = group.Prefab,
                    Start = group.Start,
                    Count = group.Count,
                    Type = group.SetValue ? typeIndex : TypeIndex.Null,
                    SharedComponentIndex = group.SetValue ? access->InsertSharedComponent_Unmanaged(group.Value) : 0,
                };
            }

            if (StructuralChangesProfiler.Enabled)
            {
                access->StructuralChangesRecorder.Begin(StructuralChangesProfiler.StructuralChangeType.CreateEntity, in access->m_WorldUnmanaged);
            }

            EntityComponentStoreInstantiate.InstantiateExistingGroups(
                entityComponentStore, (Entity*)entities.GetUnsafeReadOnlyPtr(), (EntityComponentStoreInstantiate.ExistingGroup*)existing.GetUnsafeReadOnlyPtr(),
                existing.Length);

            if (StructuralChangesProfiler.Enabled)
            {
                access->StructuralChangesRecorder.End();
            }

            access->EndStructuralChanges(ref changes);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void CheckGroupRange(int start, int count, int length)
        {
            if (start < 0 || count < 0 || start + count > length)
            {
                throw new ArgumentException($"An instantiate group's range {start} + {count} leaves the {length} reserved entities.");
            }
        }

        // A value that lands on no entity is a wrong prefab or type: the call would silently be a plain Instantiate.
        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        private static void AssertGroupHasSharedComponent<T>(EntityComponentStore* entityComponentStore, Entity srcEntity)
            where T : unmanaged, ISharedComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var linkedGroupType = TypeManager.GetTypeIndex<LinkedEntityGroup>();
            if (entityComponentStore->HasComponent(srcEntity, linkedGroupType, out _))
            {
                var header = (BufferHeader*)entityComponentStore->GetComponentDataWithTypeRO(srcEntity, linkedGroupType);
                var entities = (Entity*)BufferHeader.GetElementPointer(header);
                for (var i = 0; i < header->Length; i++)
                {
                    if (entityComponentStore->HasComponent(entities[i], typeIndex, out _))
                    {
                        return;
                    }
                }
            }
            else if (entityComponentStore->HasComponent(srcEntity, typeIndex, out _))
            {
                return;
            }

            var entityName = srcEntity.ToFixedString();
            var typeName = typeIndex.ToFixedString();
            throw new ArgumentException(
                $"Neither {entityName} nor its LinkedEntityGroup has {typeName}, so instantiating it with that shared component would set nothing.");
        }

        internal static SharedComponentDataFromIndex<T> GetSharedComponentDataFromIndex<T>(this EntityManager entityManager, bool isReadOnly = true)
            where T : unmanaged, ISharedComponentData
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new SharedComponentDataFromIndex<T>(typeIndex, access, isReadOnly);
#else
            return new SharedComponentDataFromIndex<T>(typeIndex, access);
#endif
        }

        internal static SharedComponentLookup<T> GetSharedComponentLookup<T>(this EntityManager entityManager, bool isReadOnly = true)
            where T : unmanaged, ISharedComponentData
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new SharedComponentLookup<T>(typeIndex, access, isReadOnly);
#else
            return new SharedComponentLookup<T>(typeIndex, access);
#endif
        }

        internal static UnsafeEntityDataAccess GetUnsafeEntityDataAccess(this EntityManager entityManager)
        {
            var access = entityManager.GetCheckedEntityDataAccess();

            return new UnsafeEntityDataAccess(access);
        }

        internal static UnsafeEnableableLookup GetUnsafeEnableableLookup(this EntityManager entityManager)
        {
            var access = entityManager.GetCheckedEntityDataAccess();

            return new UnsafeEnableableLookup(access);
        }

        internal static UnsafeComponentLookup<T> GetUnsafeComponentLookup<T>(this EntityManager entityManager, bool isReadOnly)
            where T : unmanaged, IComponentData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var access = entityManager.GetCheckedEntityDataAccess();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new UnsafeComponentLookup<T>(typeIndex, access, isReadOnly);
#else
            return new UnsafeComponentLookup<T>(typeIndex, access);
#endif
        }

        internal static UnsafeBufferLookup<T> GetUnsafeBufferLookup<T>(this EntityManager entityManager, bool isReadOnly)
            where T : unmanaged, IBufferElementData
        {
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var access = entityManager.GetCheckedEntityDataAccess();

            return new UnsafeBufferLookup<T>(typeIndex, access, isReadOnly);
        }

        // Internal because this is not safe called directly form EntityManager
        internal static ChangeFilterLookup<T> GetChangeFilterLookup<T>(this EntityManager entityManager, bool isReadOnly)
            where T : unmanaged
        {
            var access = entityManager.GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            return new ChangeFilterLookup<T>(typeIndex, access, isReadOnly);
#else
            return new ChangeFilterLookup<T>(typeIndex, access);
#endif
        }
    }
}
