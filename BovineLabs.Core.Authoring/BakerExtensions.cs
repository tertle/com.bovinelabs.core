// <copyright file="BakerExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using System;
    using Unity.Entities;
    using UnityEngine;
    using Object = UnityEngine.Object;

    public static class BakerExtensions
    {
        public static void AddEnabledComponent<T>(this IBaker baker, Entity entity, bool initialState)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            baker.AddComponent<T>(entity);
            baker.SetComponentEnabled<T>(entity, initialState);
        }

        public static void AddEnabledComponent<T>(this IBaker baker, Entity entity, T defaultValue, bool initialState)
            where T : unmanaged, IComponentData, IEnableableComponent
        {
            baker.AddComponent(entity, defaultValue);
            baker.SetComponentEnabled<T>(entity, initialState);
        }

        public static DynamicBuffer<T> AddEnabledBuffer<T>(this IBaker baker, Entity entity, bool initialState)
            where T : unmanaged, IBufferElementData, IEnableableComponent
        {
            var buffer = baker.AddBuffer<T>(entity);
            baker.SetComponentEnabled<T>(entity, initialState);
            return buffer;
        }

        public static Entity GetEntity(this IBaker baker, Object obj, TransformUsageFlags flags)
        {
            return obj switch
            {
                GameObject go => baker.GetEntity(go, flags),
                Component component => baker.GetEntity(component, flags),
                _ => throw new ArgumentOutOfRangeException(nameof(obj), obj, null),
            };
        }
    }
}
