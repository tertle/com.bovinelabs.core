// <copyright file="PrefabInstance.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if BL_PREFAB_INSTANCE
namespace BovineLabs.Core.Authoring.BakeFast
{
    using Unity.Collections;
    using Unity.Entities;
    using Unity.Mathematics;
    using Unity.Transforms;
    using UnityEditor;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    [BakingType]
    public struct PrefabInstanceBake : IComponentData
    {
        public Entity Prefab;
        public float4x4 Transform;
        public bool IsStatic;
    }

    public partial class PrefabInstance : MonoBehaviour
    {
        public GameObject? Prefab;

        [MenuItem(EditorMenus.RootMenu + "Utility/Create Prefab Instances")]
        public static void CreatePrefabInstances()
        {
            foreach (var g in Selection.gameObjects)
            {
                if (g.GetComponent<PrefabInstance>() != null)
                {
                    continue;
                }

                if (g.gameObject.scene.name == null)
                {
                    continue;
                }

                var prefab = PrefabUtility.GetCorrespondingObjectFromSource(g);
                if (prefab == null)
                {
                    // todo create prefab
                }
                else
                {
                    var instance = new GameObject(g.name);
                    SceneManager.MoveGameObjectToScene(instance, g.gameObject.scene);

                    instance.AddComponent<PrefabInstance>().Prefab = prefab;
                    instance.transform.parent = g.transform.parent;
                    instance.transform.localPosition = g.transform.localPosition;
                    instance.transform.localRotation = g.transform.localRotation;
                    instance.transform.localScale = g.transform.localScale;

                    DestroyImmediate(g);
                }
            }
        }

        private class Baker : Baker<PrefabInstance>
        {
            public override void Bake(PrefabInstance authoring)
            {
                var entity = this.GetEntity(TransformUsageFlags.WorldSpace);
                this.AddComponent<BakingOnlyEntity>(entity);

                var prefab = this.GetEntity(authoring.Prefab, TransformUsageFlags.None);

                this.AddComponent(entity, new PrefabInstanceBake
                {
                    Prefab = prefab,
                    Transform = authoring.transform.localToWorldMatrix,
                    IsStatic = authoring.gameObject.isStatic || authoring.gameObject.GetComponent<StaticOptimizeEntity>() != null,
                });

                this.DependsOn(authoring.transform);
            }
        }

        [UpdateInGroup(typeof(PostBakingSystemGroup))]
        [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
        private partial struct System : ISystem
        {
            private NativeHashMap<EntityGuid, Instance> spawned;

            public void OnCreate(ref SystemState state)
            {
                this.spawned = new NativeHashMap<EntityGuid, Instance>(0, Allocator.Persistent);
            }

            public void OnDestroy(ref SystemState state)
            {
                this.spawned.Dispose();
            }

            public void OnUpdate(ref SystemState state)
            {
                var query = SystemAPI.QueryBuilder().WithAll<PrefabInstanceBake>().Build();
                var prefabInstance = query.ToComponentDataArray<PrefabInstanceBake>(state.WorldUpdateAllocator);
                var entities = query.ToEntityArray(state.WorldUpdateAllocator);

                var em = state.EntityManager;

                for (var j = 0; j < prefabInstance.Length; j++)
                {
                    var guid = em.GetComponentData<EntityGuid>(entities[j]);
                    var prefab = prefabInstance[j];

                    var prefabGUID = em.GetComponentData<EntityGuid>(prefab.Prefab);

                    var instance = Entity.Null;

                    if (this.spawned.TryGetValue(guid, out var inst))
                    {
                        if (inst.PrefabGUID == prefabGUID)
                        {
                            instance = inst.Value;
                        }
                        else
                        {
                            em.DestroyEntity(inst.Value);
                        }
                    }

                    if (instance == Entity.Null)
                    {
                        if (prefab.Prefab == Entity.Null)
                        {
                            continue;
                        }

                        instance = em.Instantiate(prefab.Prefab);

                        this.spawned[guid] = new Instance { PrefabGUID = prefabGUID, Value = instance };

                        guid.b += 1;
                        em.SetComponentData(instance, guid);

                        if (em.HasBuffer<LinkedEntityGroup>(instance))
                        {
                            var leg = em.GetBuffer<LinkedEntityGroup>(instance).ToNativeArray(Allocator.Temp);

                            for (var index = 1; index < leg.Length; index++)
                            {
                                guid.b += 1;
                                em.SetComponentData(leg[index].Value, guid);
                            }
                        }
                    }

                    var matrix = prefab.Transform;

                    em.AddComponentData(instance, new LocalToWorld { Value = prefab.Transform });

                    if (prefab.IsStatic)
                    {
                        em.RemoveComponent<LocalTransform>(instance);
                        em.RemoveComponent<Child>(instance);
                        em.AddComponent<Static>(instance);

                        if (em.HasBuffer<LinkedEntityGroup>(instance))
                        {
                            var leg1 = em.GetBuffer<LinkedEntityGroup>(instance).ToNativeArray(Allocator.Temp);
                            var leg2 = em.GetBuffer<LinkedEntityGroup>(prefab.Prefab).ToNativeArray(Allocator.Temp);

                            for (var i = 1; i < leg1.Length; i++)
                            {
                                var linkedEntity = leg1[i].Value;

                                em.RemoveComponent<LocalTransform>(linkedEntity);
                                em.AddComponent<Static>(linkedEntity);
                                em.RemoveComponent<Parent>(linkedEntity);
                                em.RemoveComponent<PreviousParent>(linkedEntity);

                                var ltw = em.GetComponentData<LocalToWorld>(leg2[i].Value);
                                em.SetComponentData(linkedEntity, new LocalToWorld { Value = math.mul(prefab.Transform, ltw.Value) });
                            }
                        }
                    }
                    else
                    {
                        var scaleX = math.length(matrix.c0.xyz);
                        var scaleY = math.length(matrix.c1.xyz);
                        var scaleZ = math.length(matrix.c2.xyz);
                        if (math.abs(scaleX - scaleY) > 0.0001f || math.abs(scaleX - scaleZ) > 0.0001f)
                        {
                            var normalizedRotationMatrix = math.orthonormalize(new float3x3(matrix));
                            var rotation = new quaternion(normalizedRotationMatrix);
                            em.AddComponentData(instance, LocalTransform.FromPositionRotation(matrix.c3.xyz, rotation));

                            var compositeScale = float4x4.Scale(new float3(scaleX, scaleY, scaleZ));
                            em.AddComponentData(instance, new PostTransformMatrix { Value = compositeScale });
                        }
                        else
                        {
                            em.AddComponentData(instance, LocalTransform.FromMatrix(prefab.Transform));
                        }
                    }
                }
            }

            private struct Instance
            {
                public EntityGuid PrefabGUID;
                public Entity Value;
            }
        }
    }
}
#endif
