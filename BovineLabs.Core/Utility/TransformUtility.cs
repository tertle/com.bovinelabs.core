// <copyright file="TransformUtility.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using System;
    using BovineLabs.Core.EntityCommands;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    /// <summary> Transform hierarchy utilities for parent-child relationships. </summary>
    public static class TransformUtility
    {
        public static void SetupParent<T>(
            ref T commands, Entity parent, Entity child, in LocalToWorld parentLocalToWorld, in LocalTransform childLocalTransform, DynamicBuffer<Child> childs)
            where T : IEntityCommands
        {
            // Setup the child
            commands.Entity = child;
            commands.AddComponent(new ComponentTypeSet(ComponentType.ReadWrite<Parent>(), ComponentType.ReadWrite<PreviousParent>()));
            commands.SetComponent(new Parent { Value = parent });
            commands.SetComponent(new PreviousParent { Value = parent });
            commands.SetComponent(new LocalToWorld { Value = math.mul(parentLocalToWorld.Value, childLocalTransform.ToMatrix()) });

            // Setup the parent
            commands.Entity = parent;
            if (childs.IsCreated)
            {
                commands.AppendToBuffer(new Child { Value = child });
            }
            else
            {
                commands.AddBuffer<Child>().Add(new Child { Value = child });
            }
        }

        /// <summary>
        /// Computes and sets <see cref="LocalToWorld"/> for all entities in a <see cref="LinkedEntityGroup"/> using the current
        /// <see cref="LocalTransform"/>, <see cref="Parent"/>, and optional <see cref="PostTransformMatrix"/> values.
        /// </summary>
        /// <param name="linkedEntityGroup">The linked entity group to process.</param>
        /// <param name="localTransformLookup">Lookup for <see cref="LocalTransform"/>.</param>
        /// <param name="parentLookup">Lookup for <see cref="Parent"/>.</param>
        /// <param name="postTransformMatrixLookup">Lookup for <see cref="PostTransformMatrix"/>.</param>
        /// <param name="localToWorldLookup">Lookup for <see cref="LocalToWorld"/> (must be writable).</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an entity (or one of its ancestors) is missing the required <see cref="LocalTransform"/> component, or is missing
        /// <see cref="LocalToWorld"/>.
        /// </exception>
        public static void SetupLocalToWorld(
            DynamicBuffer<LinkedEntityGroup> linkedEntityGroup, ref ComponentLookup<LocalTransform> localTransformLookup, ref ComponentLookup<Parent> parentLookup,
            ref ComponentLookup<PostTransformMatrix> postTransformMatrixLookup, ref ComponentLookup<LocalToWorld> localToWorldLookup)
        {
            var leg = linkedEntityGroup.AsNativeArray();
            var localToWorldCache = new NativeHashMap<Entity, float4x4>(leg.Length, Allocator.Temp);
            using var scratchPool = PooledNativeList<Entity>.Make();
            var scratch = scratchPool.List;

            for (var i = 0; i < leg.Length; i++)
            {
                var entity = leg[i].Value;
                if (!localToWorldLookup.HasComponent(entity))
                {
                    continue;
                }

                var worldMatrix = ComputeWorldTransformMatrixCached(
                    entity,
                    ref localTransformLookup,
                    ref parentLookup,
                    ref postTransformMatrixLookup,
                    ref localToWorldCache,
                    ref scratch);

                localToWorldLookup[entity] = new LocalToWorld { Value = worldMatrix };
            }
        }

        /// <summary>
        /// Computes and sets <see cref="LocalToWorld"/> for all entities in a <see cref="LinkedEntityGroup"/> using the current
        /// <see cref="LocalTransform"/>, <see cref="Parent"/>, and optional <see cref="PostTransformMatrix"/> values.
        /// </summary>
        /// <param name="linkedEntityGroup">The linked entity group to process.</param>
        /// <param name="state">The system state.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown if an entity (or one of its ancestors) is missing the required <see cref="LocalTransform"/> component, or is missing
        /// <see cref="LocalToWorld"/>.
        /// </exception>
        public static void SetupLocalToWorld(DynamicBuffer<LinkedEntityGroup> linkedEntityGroup, ref SystemState state)
        {
            var leg = linkedEntityGroup.AsNativeArray();
            var localToWorldCache = new NativeHashMap<Entity, float4x4>(leg.Length, Allocator.Temp);
            using var scratchPool = PooledNativeList<Entity>.Make();
            var scratch = scratchPool.List;

            for (var i = 0; i < leg.Length; i++)
            {
                var entity = leg[i].Value;
                if (!state.EntityManager.HasComponent<LocalToWorld>(entity))
                {
                    continue;
                }

                var worldMatrix = ComputeWorldTransformMatrixCached(entity, ref state, ref localToWorldCache, ref scratch);

                state.EntityManager.SetComponentData(entity, new LocalToWorld { Value = worldMatrix });
            }
        }

        private static float4x4 ComputeWorldTransformMatrixCached(
            Entity entity, ref ComponentLookup<LocalTransform> localTransformLookup, ref ComponentLookup<Parent> parentLookup,
            ref ComponentLookup<PostTransformMatrix> postTransformMatrixLookup, ref NativeHashMap<Entity, float4x4> localToWorldCache,
            ref NativeList<Entity> scratch)
        {
            if (localToWorldCache.TryGetValue(entity, out var cached))
            {
                return cached;
            }

            scratch.Clear();

            const int maxDepth = 1024;
            var depth = 0;

            var current = entity;
            float4x4 baseMatrix;

            while (true)
            {
                if (localToWorldCache.TryGetValue(current, out baseMatrix))
                {
                    break;
                }

                scratch.Add(current);

                if (!parentLookup.TryGetComponent(current, out var parent))
                {
                    baseMatrix = float4x4.identity;
                    break;
                }

                current = parent.Value;

                depth++;
                if (depth > maxDepth)
                {
                    throw new InvalidOperationException("Parent hierarchy exceeded max depth; hierarchy may contain a cycle.");
                }
            }

            var worldMatrix = baseMatrix;
            for (var i = scratch.Length - 1; i >= 0; i--)
            {
                current = scratch[i];

                if (!localTransformLookup.TryGetComponent(current, out var localTransform))
                {
                    throw new InvalidOperationException($"Entity {current} does not have the required LocalTransform component");
                }

                worldMatrix = math.mul(worldMatrix, localTransform.ToMatrix());

                if (postTransformMatrixLookup.TryGetComponent(current, out var postTransformMatrix))
                {
                    worldMatrix = math.mul(worldMatrix, postTransformMatrix.Value);
                }

                CacheAdd(ref localToWorldCache, current, worldMatrix);
            }

            return worldMatrix;
        }

        private static float4x4 ComputeWorldTransformMatrixCached(Entity entity, ref SystemState state,
            ref NativeHashMap<Entity, float4x4> localToWorldCache, ref NativeList<Entity> scratch)
        {
            if (localToWorldCache.TryGetValue(entity, out var cached))
            {
                return cached;
            }

            scratch.Clear();

            const int maxDepth = 1024;
            var depth = 0;

            var current = entity;
            float4x4 baseMatrix;

            while (true)
            {
                if (localToWorldCache.TryGetValue(current, out baseMatrix))
                {
                    break;
                }

                scratch.Add(current);

                if (!state.EntityManager.HasComponent<Parent>(current))
                {
                    baseMatrix = float4x4.identity;
                    break;
                }

                var parent = state.EntityManager.GetComponentData<Parent>(current);

                current = parent.Value;

                depth++;
                if (depth > maxDepth)
                {
                    throw new InvalidOperationException("Parent hierarchy exceeded max depth; hierarchy may contain a cycle.");
                }
            }

            var worldMatrix = baseMatrix;
            for (var i = scratch.Length - 1; i >= 0; i--)
            {
                current = scratch[i];

                if (!state.EntityManager.HasComponent<LocalTransform>(current))
                {
                    throw new InvalidOperationException($"Entity {current} does not have the required LocalTransform component");
                }

                var localTransform = state.EntityManager.GetComponentData<LocalTransform>(current);

                worldMatrix = math.mul(worldMatrix, localTransform.ToMatrix());

                if (state.EntityManager.HasComponent<PostTransformMatrix>(current))
                {
                    var postTransformMatrix = state.EntityManager.GetComponentData<PostTransformMatrix>(current);

                    worldMatrix = math.mul(worldMatrix, postTransformMatrix.Value);
                }

                CacheAdd(ref localToWorldCache, current, worldMatrix);
            }

            return worldMatrix;
        }

        private static void CacheAdd(ref NativeHashMap<Entity, float4x4> cache, Entity entity, float4x4 worldMatrix)
        {
            if (cache.TryAdd(entity, worldMatrix) || cache.ContainsKey(entity))
            {
                return;
            }

            cache.Capacity = math.max(cache.Capacity * 2, cache.Capacity + 1);
            cache.TryAdd(entity, worldMatrix);
        }
    }
}
