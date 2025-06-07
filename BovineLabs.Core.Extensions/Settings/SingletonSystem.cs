// <copyright file="AutoSingletonSystem.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System.Collections.Generic;
    using BovineLabs.Core.Extensions;
    using BovineLabs.Core.Groups;
    using Unity.Burst;
    using Unity.Collections;
    using Unity.Entities;

    [UpdateInGroup(typeof(AfterSceneSystemGroup), OrderFirst = true)]
    public unsafe partial struct SingletonSystem : ISystem
    {
        private NativeArray<Data> types;

        public void OnCreate(ref SystemState state)
        {
            var components = AllSingletonTypes.Components;

            this.types = new NativeArray<Data>(components.Count, Allocator.Persistent);

            var queries = new NativeArray<EntityQuery>(components.Count, Allocator.Temp);
            var builder = new EntityQueryBuilder(Allocator.Temp);

            for (var index = 0; index < components.Count; index++)
            {
                var c = components[index];

                var entity = state.EntityManager.CreateEntity(c, typeof(Singleton), typeof(SingletonInitialize));
                state.EntityManager.SetComponentEnabled<SingletonInitialize>(entity, false);

                builder.Reset();
                var query = builder.WithAll(c).WithNone<Singleton>().Build(ref state);

                this.types[index] = new Data
                {
                    ComponentType = c,
                    Entity = entity,
                    Query = query,
                    TypeHandle = state.GetDynamicComponentTypeHandle(c),
                };

                queries[index] = query;
            }

            state.RequireAnyForUpdate(queries);
        }

        public void OnDestroy(ref SystemState state)
        {
            this.types.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            foreach (var t in this.types)
            {
                var data = t;
                if (data.Query.IsEmptyIgnoreFilter)
                {
                    continue;
                }

                var targetBuffer = state.EntityManager.GetUntypedBuffer(data.Entity, data.ComponentType);
                data.TypeHandle.Update(ref state);

                foreach (var chunk in data.Query.ToArchetypeChunkArray(state.WorldUpdateAllocator))
                {
                    var buffers = chunk.GetDynamicBufferAccessor(ref data.TypeHandle);

                    for (var entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                    {
                        var buffer = buffers.GetUntypedBuffer(entityIndex);
                        var ptr = (byte*)buffer.GetUnsafePtr();

                        targetBuffer.AddRange(ptr, buffer.Length);
                    }
                }

                state.EntityManager.SetComponentEnabled<SingletonInitialize>(data.Entity, true);
                state.EntityManager.RemoveComponent(data.Query, data.ComponentType);
            }
        }

        private struct Data
        {
            public ComponentType ComponentType;
            public Entity Entity;
            public EntityQuery Query;
            public DynamicComponentTypeHandle TypeHandle;
        }

        private static class AllSingletonTypes
        {
            public static readonly List<ComponentType> Components = new();

            static AllSingletonTypes()
            {
                foreach (var t in TypeManager.AllTypes)
                {
                    if (t.Category != TypeManager.TypeCategory.BufferData)
                    {
                        continue;
                    }

                    if (!t.Type.IsDefined(typeof(SingletonAttribute), true))
                    {
                        continue;
                    }

                    Components.Add(ComponentType.FromTypeIndex(t.TypeIndex));
                }
            }
        }
    }
}