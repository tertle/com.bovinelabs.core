// <copyright file="ConfigVarContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Basics.ConfigVars
{
    using System;
    using Unity.Burst;
    using Unity.Collections;
    using UnityEngine;

    /// <summary> The config var container interface that ensures conversion to string for editor settings. </summary>
    internal interface IConfigVarContainer
    {
        /// <summary>
        /// Gets or sets the config var.
        /// </summary>
        string Value { get; set; }
    }

    /// <summary> Container for setting config var static fields. </summary>
    /// <remarks> This should only be used in debugging tools. </remarks>
    /// <typeparam name="T">The type of shared static.</typeparam>
    internal class ConfigVarContainer<T> : IConfigVarContainer
        where T : struct
    {
        private readonly SharedStatic<T> field;

        /// <summary> Initializes a new instance of the <see cref="ConfigVarContainer{T}"/> class. </summary>
        /// <param name="field"> The field associated with the config var. </param>
        public ConfigVarContainer(SharedStatic<T> field)
        {
            this.field = field;
        }

        /// <summary> Gets or sets the value of the config var. </summary>
        public T DirectValue
        {
            get => this.field.Data;
            set => this.field.Data = value;
        }

        /// <inheritdoc/>
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

    /// <summary> Container for setting config var static fields that use FixedString. </summary>
    /// <remarks> This should only be used in debugging tools. </remarks>
    /// <typeparam name="T">The type of shared static. Must be one of the FixedString types. </typeparam>
    internal class ConfigVarStringContainer<T> : IConfigVarContainer
        where T : struct
    {
        private readonly SharedStatic<T> field;

        /// <summary> Initializes a new instance of the <see cref="ConfigVarStringContainer{T}"/> class. </summary>
        /// <param name="field"> The field associated with the config var. </param>
        public ConfigVarStringContainer(SharedStatic<T> field)
        {
            this.field = field;
        }

        /// <inheritdoc/>
        public string Value
        {
            get
            {
                switch (this.field)
                {
                    case SharedStatic<FixedString32> s32:
                        return s32.Data.ToString();
                    case SharedStatic<FixedString64> s64:
                        return s64.Data.ToString();
                    case SharedStatic<FixedString128> s128:
                        return s128.Data.ToString();
                    case SharedStatic<FixedString512> s512:
                        return s512.Data.ToString();
                    case SharedStatic<FixedString4096> s4096:
                        return s4096.Data.ToString();
                    default:
                        throw new InvalidOperationException("String config var must be a FixedString type");
                }
            }

            set
            {
                switch (this.field)
                {
                    case SharedStatic<FixedString32> s32:
                        s32.Data = new FixedString32(value);
                        break;
                    case SharedStatic<FixedString64> s64:
                        s64.Data = new FixedString64(value);
                        break;
                    case SharedStatic<FixedString128> s128:
                        s128.Data = new FixedString64(value);
                        break;
                    case SharedStatic<FixedString512> s512:
                        s512.Data = new FixedString64(value);
                        break;
                    case SharedStatic<FixedString4096> s4096:
                        s4096.Data = new FixedString64(value);
                        break;
                    default:
                        throw new InvalidOperationException("String config var must be a FixedString type");
                }
            }
        }
    }
}