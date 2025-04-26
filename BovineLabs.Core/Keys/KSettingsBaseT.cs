// <copyright file="KSettingsBaseT.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Keys
{
    using System.Collections.Generic;

    /// <summary>
    /// <see cref="KSettingsBase"/> with the value Type, this is used for custom tooling but should not be implemented directly.
    /// Instead implement <see cref="KSettings{T,TV}" /> or rarely <see cref="KSettingsBase{T,TV}" />.
    /// </summary>
    /// <typeparam name="TV"> The value. </typeparam>
    public abstract class KSettingsBase<TV> : KSettingsBase
    {
        public abstract IEnumerable<NameValue<TV>> Keys { get; }
    }

}
