namespace BovineLabs.Core.Utility
{
    using System;
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;

    public static class TransformUtility
    {
        public static void SetupLocalToWorld(
            DynamicBuffer<LinkedEntityGroup> linkedEntityGroup, ref ComponentLookup<LocalTransform> localTransformLookup,
            ref ComponentLookup<Parent> parentLookup, ref ComponentLookup<PostTransformMatrix> postTransformMatrixLookup,
            ref ComponentLookup<LocalToWorld> localToWorldLookup)
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

        private static float4x4 ComputeWorldTransformMatrixCached(
            Entity entity, ref SystemState state, ref NativeHashMap<Entity, float4x4> localToWorldCache, ref NativeList<Entity> scratch)
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
