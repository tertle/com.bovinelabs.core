// <copyright file="ConfigVarSharedStaticContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using UnityEngine;

    /// <summary> Container for setting config var static fields. </summary>
    /// <remarks> This should only be used in debugging tools. </remarks>
    /// <typeparam name="T"> The type of shared static. </typeparam>
    internal class ConfigVarSharedStaticContainer<T> : IConfigVarContainer
        where T : struct
    {
        private readonly SharedStatic<T> field;

        /// <summary> Initializes a new instance of the <see cref="ConfigVarSharedStaticContainer{T}" /> class. </summary>
        /// <param name="field"> The field associated with the config var. </param>
        public ConfigVarSharedStaticContainer(SharedStatic<T> field)
        {
            this.field = field;
        }

        /// <summary> Gets or sets the value of the config var. </summary>
        public T DirectValue
        {
            get => this.field.Data;
            set => this.field.Data = value;
        }

        /// <inheritdoc />
        string IConfigVarContainer.Value
        {
            get => this.DirectValue.ToString();
            set
            {
                try
                {
                    this.DirectValue = (T)Convert.ChangeType(value, typeof(T));
                }
                catch (Exception)
                {
                    Debug.LogWarning($"Trying to set a config value of {value} which is not of type {typeof(T)}");
                }
            }
        }
    }
}
