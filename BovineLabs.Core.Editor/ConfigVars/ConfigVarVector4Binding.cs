// <copyright file="ConfigVarVector4Binding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class ConfigVarVector4Binding : ConfigVarBindingBase<Vector4>
    {
        public ConfigVarVector4Binding(BaseField<Vector4> baseField, ConfigVarAttribute attribute, SharedStatic<Vector4> sharedStatic)
            : base(baseField, attribute, new ConfigVarSharedStaticVector4Container(sharedStatic))
        {
        }
    }
}
