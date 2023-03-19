// <copyright file="BakerExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Authoring
{
    using Unity.Entities;

    public static class BakerExtensions
    {
        public static void SetComponent<T>(this IBaker baker, T component)
            where T : unmanaged, IComponentData
        {
            baker.SetComponent(baker.GetEntity(), component);
        }
    }
}
