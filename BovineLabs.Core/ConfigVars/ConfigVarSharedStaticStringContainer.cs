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
    /// <typeparam name="T"> The type of shared static. Must be one of the FixedString types. </typeparam>
    internal class ConfigVarSharedStaticStringContainer<T> : IConfigVarContainer<T>
        where T : unmanaged
    {
        private readonly SharedStatic<T> field;

        /// <summary> Initializes a new instance of the <see cref="ConfigVarSharedStaticStringContainer{T}" /> class. </summary>
        /// <param name="field"> The field associated with the config var. </param>
        public ConfigVarSharedStaticStringContainer(SharedStatic<T> field)
        {
            this.field = field;
        }

        T IConfigVarContainer<T>.Value
        {
            get => this.field.Data;
            set => this.field.Data = value;
        }

        /// <inheritdoc />
        string IConfigVarContainer.StringValue
        {
            get => this.field switch
            {
                SharedStatic<FixedString32Bytes> s32 => s32.Data.ToString(),
                SharedStatic<FixedString64Bytes> s64 => s64.Data.ToString(),
                SharedStatic<FixedString128Bytes> s128 => s128.Data.ToString(),
                SharedStatic<FixedString512Bytes> s512 => s512.Data.ToString(),
                SharedStatic<FixedString4096Bytes> s4096 => s4096.Data.ToString(),
                _ => throw new InvalidOperationException("String config var must be a FixedString type"),
            };

            set
            {
                switch (this.field)
                {
                    case SharedStatic<FixedString32Bytes> s32:
                        s32.Data = new FixedString32Bytes(value);

                        break;
                    case SharedStatic<FixedString64Bytes> s64:
                        s64.Data = new FixedString64Bytes(value);

                        break;
                    case SharedStatic<FixedString128Bytes> s128:
                        s128.Data = new FixedString128Bytes(value);

                        break;
                    case SharedStatic<FixedString512Bytes> s512:
                        s512.Data = new FixedString512Bytes(value);

                        break;
                    case SharedStatic<FixedString4096Bytes> s4096:
                        s4096.Data = new FixedString4096Bytes(value);

                        break;
                    default:
                        throw new InvalidOperationException("String config var must be a FixedString type");
                }
            }
        }

        public Type Type
        {
            get
            {
                return this.field switch
                {
                    SharedStatic<FixedString32Bytes> => typeof(FixedString32Bytes),
                    SharedStatic<FixedString64Bytes> => typeof(FixedString64Bytes),
                    SharedStatic<FixedString128Bytes> => typeof(FixedString128Bytes),
                    SharedStatic<FixedString512Bytes> => typeof(FixedString512Bytes),
                    SharedStatic<FixedString4096Bytes> => typeof(FixedString4096Bytes),
                    _ => throw new InvalidOperationException("String config var must be a FixedString type"),
                };
            }
        }
    }
}
