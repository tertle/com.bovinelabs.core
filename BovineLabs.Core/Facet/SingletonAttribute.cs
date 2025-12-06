// <copyright file="SingletonAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;

    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SingletonAttribute : Attribute
    {
    }
}
