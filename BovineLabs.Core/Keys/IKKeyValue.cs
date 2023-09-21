// <copyright file="IKKeyValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    public interface IKKeyValue
    {
        string Name { get; set; }

        int Value { get; set; }
    }
}
