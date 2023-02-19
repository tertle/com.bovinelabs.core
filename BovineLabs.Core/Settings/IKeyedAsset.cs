// <copyright file="IKeyedAsset.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    public interface IKeyedAsset
    {
        public int Key { get; protected internal set; }
    }
}
