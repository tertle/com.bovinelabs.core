// <copyright file="SaveManager.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Serialization
{
    using System;
    using System.Threading.Tasks;
    using Unity.Entities;

    /// <summary> Manages save files. </summary>
    public class SaveManager : IDisposable
    {
        /// <summary> Try and load a save from a slot. </summary>
        /// <param name="world"> The world to load onto. </param>
        /// <param name="slot"> The slot to load from. </param>
        /// <returns> True if loaded successfully. </returns>
        public Task<bool> TryLoadSave(World world, int slot)
        {
            // TODO world destroyed? separate operations
            return Task.FromResult(false);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }
    }
}