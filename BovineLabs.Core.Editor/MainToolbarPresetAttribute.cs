// <copyright file="MainToolbarPresetAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if UNITY_6000_3_OR_NEWER
namespace BovineLabs.Core.Editor
{
    using System;

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class MainToolbarPresetAttribute : Attribute
    {
    }
}
#endif
