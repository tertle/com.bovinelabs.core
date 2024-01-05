// <copyright file="IBindingObject.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    public interface IBindingObject
    {
    }

    public interface IBindingObject<T> : IBindingObject
        where T : unmanaged
    {
        ref T Value { get; }

        void Load()
        {
        }

        void Unload()
        {
        }
    }
}
#endif
