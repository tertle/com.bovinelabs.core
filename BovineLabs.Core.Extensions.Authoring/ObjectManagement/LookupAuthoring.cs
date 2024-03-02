// <copyright file="LookupAuthoring.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.Authoring.ObjectManagement
{
    using System;
    using System.Collections.Generic;
    using BovineLabs.Core.Iterators;
    using BovineLabs.Core.ObjectManagement;
    using Unity.Entities;
    using UnityEngine;

    internal interface ILookupAuthoring
    {
        void Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map);
    }

    public abstract class LookupAuthoring<TMap, TValue> : MonoBehaviour, ILookupAuthoring
        where TMap : unmanaged, IDynamicHashMap<ObjectId, TValue>
        where TValue : unmanaged
    {
        void ILookupAuthoring.Bake(IBaker baker, Entity entity, ObjectDefinition id, Dictionary<Type, object> map)
        {
            if (!map.TryGetValue(typeof(TMap), out var wrapper))
            {
                map[typeof(TMap)] = wrapper = new ManagedBuffer(baker, entity);
            }

            var genericWrapper = (ManagedBuffer)wrapper;
            genericWrapper.Add(id, this.GetValue(baker));
        }

        protected abstract TValue GetValue(IBaker baker);

        private class ManagedBuffer
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
    }
}
#endif
