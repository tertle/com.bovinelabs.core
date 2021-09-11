// <copyright file="IConfigVarContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    /// <summary> The config var container interface that ensures conversion to string for editor settings. </summary>
    public interface IConfigVarContainer
    {
        /// <summary> Gets or sets the config var. </summary>
        string Value { get; set; }
    }
}
