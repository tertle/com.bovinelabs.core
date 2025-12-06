// <copyright file="ConfigVarSharedStaticVector4Container.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using UnityEngine;

    public class ConfigVarSharedStaticVector4Container : IConfigVarContainer<Vector4>
    {
        private readonly SharedStatic<Vector4> field;

        /// <summary> Initializes a new instance of the <see cref="ConfigVarSharedStaticVector4Container" /> class. </summary>
        /// <param name="field"> The field associated with the config var. </param>
        public ConfigVarSharedStaticVector4Container(SharedStatic<Vector4> field)
        {
            this.field = field;
        }

        /// <inheritdoc />
        Vector4 IConfigVarContainer<Vector4>.Value
        {
            get => this.field.Data;
            set => this.field.Data = value;
        }

        /// <inheritdoc />
        string IConfigVarContainer.StringValue
        {
            get => ConfigVarAttribute.RectToVector4(this.field.Data);
            set => this.field.Data = ConfigVarAttribute.StringToVector4(value);
        }

        /// <inheritdoc/>
        public Type Type => typeof(Vector4);
    }
}
