// <copyright file="ConfigVarSharedStaticStringContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using Unity.Collections;

    /// <summary> Container for setting config var static fields that use FixedString. </summary>
    /// <remarks> This should only be used in debugging tools. </remarks>
    /// <typeparam name="T">The type of shared static. Must be one of the FixedString types. </typeparam>
    internal class ConfigVarSharedStaticStringContainer<T> : IConfigVarContainer
        where T : struct
    {
        private readonly SharedStatic<T> field;

        /// <summary> Initializes a new instance of the <see cref="ConfigVarSharedStaticStringContainer{T}"/> class. </summary>
        /// <param name="field"> The field associated with the config var. </param>
        public ConfigVarSharedStaticStringContainer(SharedStatic<T> field)
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
                value ??= string.Empty;

                switch (this.field)
                {
                    case SharedStatic<FixedString32> s32:
                        s32.Data = new FixedString32(value);
                        break;
                    case SharedStatic<FixedString64> s64:
                        s64.Data = new FixedString64(value);
                        break;
                    case SharedStatic<FixedString128> s128:
                        s128.Data = new FixedString128(value);
                        break;
                    case SharedStatic<FixedString512> s512:
                        s512.Data = new FixedString512(value);
                        break;
                    case SharedStatic<FixedString4096> s4096:
                        s4096.Data = new FixedString4096(value);
                        break;
                    default:
                        throw new InvalidOperationException("String config var must be a FixedString type");
                }
            }
        }
    }
}
