// <copyright file="ConfigVarSharedStaticColorContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using UnityEngine;

    public class ConfigVarSharedStaticColorContainer : IConfigVarContainer<Color>
    {
        private readonly SharedStatic<Color> field;

        /// <summary> Initializes a new instance of the <see cref="ConfigVarSharedStaticColorContainer" /> class. </summary>
        /// <param name="field"> The field associated with the config var. </param>
        public ConfigVarSharedStaticColorContainer(SharedStatic<Color> field)
        {
            this.field = field;
        }

        /// <inheritdoc />
        Color IConfigVarContainer<Color>.Value
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
        public Type Type => typeof(Color);
    }
}
