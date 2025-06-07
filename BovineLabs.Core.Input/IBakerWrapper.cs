// <copyright file="IBakerWrapper.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Input
{
    using Unity.Entities;
    using UnityEngine;

    public interface IBakerWrapper
    {
        void AddComponent<T>(T component)
            where T : unmanaged, IComponentData;

        T DependsOn<T>(T obj)
            where T : Object;
    }
}
