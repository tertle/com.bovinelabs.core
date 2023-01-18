// <copyright file="IConfigVarBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using UnityEngine.UIElements;

    public interface IConfigVarBinding<T> : IBinding
    {
        T Value { get; }
    }
}
