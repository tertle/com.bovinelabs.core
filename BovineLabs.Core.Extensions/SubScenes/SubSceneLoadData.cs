// <copyright file="SubSceneLoadData.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_SUBSCENE
namespace BovineLabs.Core.SubScenes
{
    using Unity.Entities;

    public struct SubSceneLoadData : IComponentData
    {
        public int ID;
        public bool WaitForLoad;
        public bool IsRequired;
        public WorldFlags TargetWorld;
    }
}
#endif
