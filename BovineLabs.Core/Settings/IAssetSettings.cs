// <copyright file="IAssetSettings.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using Unity.Entities;

    /// <summary> Interface to define a settings that is used to store managed assets which will be converted to unmanaged references at runtime. </summary>
    /// <typeparam name="T"> The component that holds the unmanaged references and is used in the world. </typeparam>
    public interface IAssetSettings<T>
        where T : struct, IComponentData
    {
        /// <summary> Convert the managed reference to the unmanaged component. </summary>
        /// <param name="config"> The component. </param>
        /// <returns> A copy of the component with updated references. </returns>
        T ApplyTo(T config);
    }
}