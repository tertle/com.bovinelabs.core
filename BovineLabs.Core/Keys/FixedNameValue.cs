// <copyright file="FixedNameValue.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using Unity.Collections;

    public struct FixedNameValue<T>
    {
        public FixedString32Bytes Name;
        public T Value;
    }
}
