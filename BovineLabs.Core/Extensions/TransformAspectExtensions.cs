// <copyright file="TransformAspectExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Assertions;
    using Unity.Collections;
    using Unity.Collections.LowLevel.Unsafe;
    using Unity.Entities;
    using Unity.Transforms;

    public static class TransformAspectExtensions
    {
        public static bool Has(ref this TransformAspect.Lookup transformAspectLookup, Entity entity)
        {
            Check.Assume(UnsafeUtility.SizeOf<TransformAspect.Lookup>() == UnsafeUtility.SizeOf<TransformAspectLookup>());
            ref var ta = ref UnsafeUtility.As<TransformAspect.Lookup, TransformAspectLookup>(ref transformAspectLookup);
            return ta.LocalTransformComponentLookup.HasComponent(entity);
        }

        public static bool TryGetAspect(ref this TransformAspect.Lookup transformAspectLookup, Entity entity, out TransformAspect transformAspect)
        {
            if (!Has(ref transformAspectLookup, entity))
            {
                transformAspect = default;
                return false;
            }

            transformAspect = transformAspectLookup[entity];
            return true;
        }

        public static bool HasWorldTransforms(ref this TransformAspect.ResolvedChunk chunk)
        {
            Check.Assume(UnsafeUtility.SizeOf<TransformAspect.ResolvedChunk>() == UnsafeUtility.SizeOf<TransformAspectResolvedChunk>());
            ref var ta = ref UnsafeUtility.As<TransformAspect.ResolvedChunk, TransformAspectResolvedChunk>(ref chunk);
            return ta.WorldTransform.Length > 0;
        }

        public static NativeArray<WorldTransform> WorldTransforms(ref this TransformAspect.ResolvedChunk chunk)
        {
            Check.Assume(UnsafeUtility.SizeOf<TransformAspect.ResolvedChunk>() == UnsafeUtility.SizeOf<TransformAspectResolvedChunk>());
            ref var ta = ref UnsafeUtility.As<TransformAspect.ResolvedChunk, TransformAspectResolvedChunk>(ref chunk);
            return ta.WorldTransform;
        }

        public static NativeArray<LocalTransform> LocalTransforms(ref this TransformAspect.ResolvedChunk chunk)
        {
            Check.Assume(UnsafeUtility.SizeOf<TransformAspect.ResolvedChunk>() == UnsafeUtility.SizeOf<TransformAspectResolvedChunk>());
            ref var ta = ref UnsafeUtility.As<TransformAspect.ResolvedChunk, TransformAspectResolvedChunk>(ref chunk);
            return ta.LocalTransform;
        }

        private struct TransformAspectLookup
        {
            public byte IsReadOnly;

            public ComponentLookup<LocalTransform> LocalTransformComponentLookup;
            public ComponentLookup<WorldTransform> WorldTransformComponentLookup;
            public ComponentLookup<ParentTransform> ParentTransformComponentLookup;
        }

        private struct TransformAspectResolvedChunk
        {
            public NativeArray<LocalTransform> LocalTransform;
            public NativeArray<WorldTransform> WorldTransform;
            public NativeArray<ParentTransform> ParentTransform;
            public int Length;
        }
    }
}
