// <copyright file="IBindingObjectHash.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_UI
namespace BovineLabs.Core.UI
{
    using UnityEngine.UIElements;

    public interface IBindingObjectHash<T> : IBindingObject<T>, IDataSourceViewHashProvider
        where T : unmanaged, IBindingObjectHashData
    {
        long IDataSourceViewHashProvider.GetViewHashCode()
        {
            return this.Value.Version;
        }
    }

    public interface IBindingObjectHashData
    {
        long Version { get; set; }
    }
}
#endif
