// <copyright file="Mod.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using Unity.Entities;
    using Unity.Properties;

    public readonly struct Mod : IComponentData
    {
        public Mod(int value)
        {
            this.Value = value;
        }

        [CreateProperty]
        public int Value { get; }
    }
}
#endif
