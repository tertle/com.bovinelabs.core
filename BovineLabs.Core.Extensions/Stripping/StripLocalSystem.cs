// <copyright file="StripLocalSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_STRIP
namespace BovineLabs.Core.Stripping
{
    using System.Reflection;
    using BovineLabs.Core.Groups;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation)]
    [UpdateInGroup(typeof(AfterSceneSystemGroup))]
    public partial struct StripLocalSystem : ISystem
    {
        private NativeArray<ComponentTypeSet> componentsToRemove;
        private NativeArray<EntityQuery> queries;

        /// <inheritdoc />
        public void OnCreate(ref SystemState state)
        {
            var allComponents = new NativeList<ComponentType>(Allocator.Temp);

            foreach (var t in TypeManager.AllTypes)
            {
                if (t.Type?.GetCustomAttribute<StripLocalAttribute>() == null)
                {
                    continue;
                }

                allComponents.Add(ComponentType.FromTypeIndex(t.TypeIndex));
            }

            const int maxCapacity = 15;
            var iterations = allComponents.Length / maxCapacity;
            var remaining = allComponents.Length % maxCapacity;

            var count = iterations + (remaining != 0 ? 1 : 0);

            this.componentsToRemove = new NativeArray<ComponentTypeSet>(count, Allocator.Persistent);
            this.queries = new NativeArray<EntityQuery>(count, Allocator.Persistent);

            var builder = new EntityQueryBuilder(Allocator.Temp);
            var components = new FixedList128Bytes<ComponentType>();
            var index = 0;

            for (var i = 0; i < iterations; i++)
            {
                var startIndex = i * maxCapacity;
                AddComponentTypes(ref components, allComponents, startIndex, maxCapacity);
                AddQuery(ref state, this.componentsToRemove, this.queries, ref builder, ref components, index++);
            }

            // Add the remainder
            if (remaining > 0)
            {
                var startIndex = iterations * maxCapacity;
                AddComponentTypes(ref components, allComponents, startIndex, remaining);
                AddQuery(ref state, this.componentsToRemove, this.queries, ref builder, ref components, index);
            }

            state.RequireAnyForUpdate(this.queries);

            return;

            static void AddComponentTypes(ref FixedList128Bytes<ComponentType> componentTypes, NativeList<ComponentType> types, int startIndex, int length)
            {
                componentTypes.Clear();

                for (var j = 0; j < length; j++)
                {
                    componentTypes.Add(types[startIndex + j]);
                }
            }

            static void AddQuery(
                ref SystemState state, NativeArray<ComponentTypeSet> componentsToRemove, NativeArray<EntityQuery> queries, ref EntityQueryBuilder builder,
                ref FixedList128Bytes<ComponentType> components, int index)
            {
                builder.Reset();
                componentsToRemove[index] = new ComponentTypeSet(components);
                queries[index] = builder
                    .WithAny(ref components)
                    .WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IgnoreComponentEnabledState)
                    .Build(ref state);
            }
        }

        /// <inheritdoc />
        public void OnDestroy(ref SystemState state)
        {
            this.componentsToRemove.Dispose();
            this.queries.Dispose();
        }

        /// <inheritdoc />
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            for (var i = 0; i < this.queries.Length; i++)
            {
                state.EntityManager.RemoveComponent(this.queries[i], this.componentsToRemove[i]);
            }
        }
    }
}
#endif
