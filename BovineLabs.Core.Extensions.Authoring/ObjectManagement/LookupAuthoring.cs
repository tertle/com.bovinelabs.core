// <copyright file="LookupAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Authoring.LifeCycle;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Entities;
    using UnityEngine;

    public interface ILookupAuthoring
    {
        void Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map);
    }

    public interface ILookupAuthoring<TMap, out TValue> : ILookupAuthoring
        where TMap : unmanaged, IDynamicHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        TValue GetValue();

        void ILookupAuthoring.Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map)
        {
            if (!map.TryGetValue(typeof(TMap), out var wrapper))
            {
                map[typeof(TMap)] = wrapper = new ManagedBuffer<TMap, TValue>(baker, entity);
            }

            var genericWrapper = (ManagedBuffer<TMap, TValue>)wrapper;
            genericWrapper.Add(id, this.GetValue());
        }
    }

    public interface ILookupMultiAuthoring<TMap, out TValue> : ILookupAuthoring
        where TMap : unmanaged, IDynamicMultiHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        TValue GetValue();

        void ILookupAuthoring.Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map)
        {
            if (!map.TryGetValue(typeof(TMap), out var wrapper))
            {
                map[typeof(TMap)] = wrapper = new ManagedMultiBuffer<TMap, TValue>(baker, entity);
            }

            var genericWrapper = (ManagedMultiBuffer<TMap, TValue>)wrapper;
            genericWrapper.Add(id, this.GetValue());
        }
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
        public abstract TValue GetValue();
    }

    [RequireComponent(typeof(LifeCycleAuthoring))]
    public abstract class LookupMultiAuthoring<TMap, TValue> : MonoBehaviour, ILookupMultiAuthoring<TMap, TValue>
        where TMap : unmanaged, IDynamicMultiHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        public abstract TValue GetValue();
    }
}
#endif
