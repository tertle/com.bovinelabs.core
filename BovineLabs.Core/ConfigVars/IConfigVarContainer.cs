// <copyright file="IConfigVarContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    /// <summary> The config var container interface that ensures conversion to string for editor settings. </summary>
    public interface IConfigVarContainer
    {
        /// <summary> Gets or sets the value of the config var. </summary>
        string StringValue { get; set; }
    }

    public interface IConfigVarContainer<T> : IConfigVarContainer
    {
        /// <summary> Gets or sets the value of the config var. </summary>
        public T Value { get; set; }
    }
}
