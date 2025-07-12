// <copyright file="SingletonAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Settings
{
    using System;

    [AttributeUsage(AttributeTargets.Struct)]
    public class SingletonAttribute : Attribute
    {
    }
}