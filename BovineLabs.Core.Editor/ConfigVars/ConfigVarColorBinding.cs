// <copyright file="ConfigVarColorBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class ConfigVarColorBinding : ConfigVarBindingBase<Color>
    {
        public ConfigVarColorBinding(BaseField<Color> baseField, ConfigVarAttribute attribute, SharedStatic<Color> sharedStatic)
            : base(baseField, attribute, new ConfigVarSharedStaticColorContainer(sharedStatic))
        {
        }
    }
}
