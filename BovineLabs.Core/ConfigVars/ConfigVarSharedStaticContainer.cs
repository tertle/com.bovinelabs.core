// <copyright file="ConfigVarSharedStaticContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;

    /// <summary> Container for setting config var static fields. </summary>
    /// <remarks> This should only be used in debugging tools. </remarks>
    /// <typeparam name="T"> The type of shared static. </typeparam>
    internal class ConfigVarSharedStaticContainer<T> : IConfigVarContainer<T>
        where T : unmanaged
    {
        private readonly SharedStatic<T> field;

        /// <summary> Initializes a new instance of the <see cref="ConfigVarSharedStaticContainer{T}" /> class. </summary>
        /// <param name="field"> The field associated with the config var. </param>
        public ConfigVarSharedStaticContainer(SharedStatic<T> field)
        {
            this.field = field;
        }

        /// <inheritdoc />
        T IConfigVarContainer<T>.Value
        {
            get => this.field.Data;
            set => this.field.Data = value;
        }

        /// <inheritdoc />
        string IConfigVarContainer.StringValue
        {
            get => this.field.Data.ToString();
            set => this.field.Data = (T)Convert.ChangeType(value, typeof(T));
        }

        public Type Type => typeof(T);
    }
}
