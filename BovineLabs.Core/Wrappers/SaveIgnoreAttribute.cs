// <copyright file="SaveIgnoreAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Savanna
{
    using System;

    /// <summary>
    /// When added to a field in a component that has an <see cref="SaveAttribute"/> it will cause the field to not be deserialized when loading.
    /// </summary>
    /// <remarks>
    /// It is much more performant to group all fields that have this attribute on them together at either the top or bottom of the struct as this
    /// will minimize the number of memory copy operations that need to be executed. Each group of serializable fields requires a separate
    /// copy so only a single call is required when they are all grouped together.
    ///
    /// Note that this attribute will still cause the entire component to be serialized and will not reduce file size, instead when deserialized the data will
    /// simply be ignored. This is done for both performance but also to allow you to change your mind. If you wanted the field to no longer be ignored
    /// you could safely remove the attribute and existing saves will have the data and deserialize correctly.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Field)]
    public class SaveIgnoreAttribute : Attribute
    {
    }
}
