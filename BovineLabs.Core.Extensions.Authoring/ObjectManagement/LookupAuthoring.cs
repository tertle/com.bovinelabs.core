// <copyright file="LookupAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION && !BL_DISABLE_LIFECYCLE
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.LifeCycle;
    using BovineLabs.Core.Collections;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Entities;
    using UnityEngine;

    public interface ILookupAuthoring
    {
        void Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map);
    }

    // TODO MOD SUPPORT
    public interface ILookupAuthoring<TMap, TValue> : ILookupAuthoring
        where TMap : unmanaged, IDynamicHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        void ILookupAuthoring.Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map)
        {
            if (!this.TryGetInitialization(out var value))
            {
                return;
            }

            if (!map.TryGetValue(typeof(TMap), out var wrapper))
            {
                BLGlobalLogger.LogErrorString($"Map not found in precomputed buffer {typeof(TMap)}");
                return;
            }

            var genericWrapper = (ManagedBuffer<TMap, TValue>)wrapper;
            genericWrapper.Add(id, value);
        }

        bool TryGetInitialization(out TValue value);
    }

    public interface ILookupAosAuthoring<TMap, TValue> : ILookupAuthoring
        where TMap : unmanaged, IAosHashMapEntry<ObjectId, TValue>
        where TValue : unmanaged
    {
        void ILookupAuthoring.Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map)
        {
            if (!this.TryGetInitialization(out var value))
            {
                return;
            }

            if (!map.TryGetValue(typeof(TMap), out var wrapper))
            {
                BLGlobalLogger.LogErrorString($"Map not found in precomputed buffer {typeof(TMap)}");
                return;
            }

            var genericWrapper = (ManagedAosBuffer<TMap, TValue>)wrapper;
            genericWrapper.Add(id, value);
        }

        bool TryGetInitialization(out TValue value);
    }

    public interface ILookupMultiAuthoring<TMap, TValue> : ILookupAuthoring
        where TMap : unmanaged, IDynamicMultiHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        void ILookupAuthoring.Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map)
        {
            if (!this.TryGetInitialization(out var value))
            {
                return;
            }

            if (!map.TryGetValue(typeof(TMap), out var wrapper))
            {
                BLGlobalLogger.LogErrorString($"Map not found in precomputed buffer {typeof(TMap)}");
                return;
            }

            var genericWrapper = (ManagedMultiBuffer<TMap, TValue>)wrapper;
            genericWrapper.Add(id, value);
        }

        bool TryGetInitialization(out TValue value);
    }

    internal class ManagedBuffer<TMap, TValue>
        where TMap : unmanaged, IDynamicHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        private DynamicHashMap<ObjectId, TValue> map;

        public ManagedBuffer(IBaker baker, Entity entity)
        {
            this.map = baker.AddBuffer<TMap>(entity).InitializeHashMap<TMap, ObjectId, TValue>().AsHashMap<TMap, ObjectId, TValue>();
        }

        public void Add(ObjectId id, TValue value)
        {
            this.map.Add(id, value);
        }
    }

    internal class ManagedAosBuffer<TMap, TValue>
        where TMap : unmanaged, IAosHashMapEntry<ObjectId, TValue>
        where TValue : unmanaged
    {
        private AosHashMap<ObjectId, TValue, TMap> map;

        public ManagedAosBuffer(IBaker baker, Entity entity)
        {
            this.map = baker.AddBuffer<TMap>(entity).AsAosHashMap<ObjectId, TValue, TMap>();
        }

        public void Add(ObjectId id, TValue value)
        {
            if (!this.map.TryAdd(id, value))
            {
                throw new ArgumentException($"An item with the same key has already been added. Key: {id}");
            }
        }
    }

    internal class ManagedMultiBuffer<TMap, TValue>
        where TMap : unmanaged, IDynamicMultiHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        private DynamicMultiHashMap<ObjectId, TValue> map;

        public ManagedMultiBuffer(IBaker baker, Entity entity)
        {
            this.map = baker.AddBuffer<TMap>(entity).InitializeMultiHashMap<TMap, ObjectId, TValue>().AsMultiHashMap<TMap, ObjectId, TValue>();
        }

        public void Add(ObjectId id, TValue value)
        {
            this.map.Add(id, value);
        }
    }

    [RequireComponent(typeof(LifeCycleAuthoring))]
    public abstract class LookupAuthoring : MonoBehaviour, ILookupAuthoring
    {
        public abstract void Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map);
    }

    [RequireComponent(typeof(LifeCycleAuthoring))]
    public abstract class LookupAuthoring<TMap, TValue> : MonoBehaviour, ILookupAuthoring<TMap, TValue>
        where TMap : unmanaged, IDynamicHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        public abstract bool TryGetInitialization(out TValue value);
    }

    [RequireComponent(typeof(LifeCycleAuthoring))]
    public abstract class LookupAosAuthoring<TMap, TValue> : MonoBehaviour, ILookupAosAuthoring<TMap, TValue>
        where TMap : unmanaged, IAosHashMapEntry<ObjectId, TValue>
        where TValue : unmanaged
    {
        public abstract bool TryGetInitialization(out TValue value);
    }

    [RequireComponent(typeof(LifeCycleAuthoring))]
    public abstract class LookupMultiAuthoring<TMap, TValue> : MonoBehaviour, ILookupMultiAuthoring<TMap, TValue>
        where TMap : unmanaged, IDynamicMultiHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        public abstract bool TryGetInitialization(out TValue value);
    }
}
#endif
