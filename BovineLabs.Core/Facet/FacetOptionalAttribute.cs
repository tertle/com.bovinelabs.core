// <copyright file="FacetOptionalAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core
{
    using System;

    /// <summary> Flags an IFacet field as optional when generating lookup and type handle accessors. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FacetOptionalAttribute : Attribute
    {
    }
}
