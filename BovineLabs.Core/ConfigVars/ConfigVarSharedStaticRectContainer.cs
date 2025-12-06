// <copyright file="ConfigVarSharedStaticRectContainer.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using UnityEngine;

    public class ConfigVarSharedStaticRectContainer : IConfigVarContainer<Rect>
    {
        private readonly SharedStatic<Rect> field;

        /// <summary> Initializes a new instance of the <see cref="ConfigVarSharedStaticRectContainer" /> class. </summary>
        /// <param name="field"> The field associated with the config var. </param>
        public ConfigVarSharedStaticRectContainer(SharedStatic<Rect> field)
        {
            this.field = field;
        }

        /// <inheritdoc />
        Rect IConfigVarContainer<Rect>.Value
        {
            get => this.field.Data;
            set => this.field.Data = value;
        }

        /// <inheritdoc />
        string IConfigVarContainer.StringValue
        {
            get => ConfigVarAttribute.RectToVector4(RectToVector4(this.field.Data));
            set => this.field.Data = Vector4ToRect(ConfigVarAttribute.StringToVector4(value));
        }

        /// <inheritdoc/>
        public Type Type => typeof(Vector4);

        private static Vector4 RectToVector4(Rect v4)
        {
            return new Vector4(v4.x, v4.y, v4.width, v4.height);
        }

        private static Rect Vector4ToRect(Vector4 v4)
        {
            return new Rect(v4.x, v4.y, v4.z, v4.y);
        }
    }
}
