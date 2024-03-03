// <copyright file="IInputSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using Unity.Entities;

    public interface IBakerWrapper
    {
        void AddComponent<T>(T component)
            where T : unmanaged, IComponentData;

        T DependsOn<T>(T obj)
            where T : UnityEngine.Object;
    }

    public interface IInputSettings
    {
        void Bake(IBakerWrapper baker);
    }
}
#endif
