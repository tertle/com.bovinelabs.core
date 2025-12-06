// <copyright file="ConfigVarRectBinding.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.ConfigVars
{
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using UnityEngine;
    using UnityEngine.UIElements;

    internal class ConfigVarRectBinding : ConfigVarBindingBase<Rect>
    {
        public ConfigVarRectBinding(BaseField<Rect> baseField, ConfigVarAttribute attribute, SharedStatic<Rect> sharedStatic)
            : base(baseField, attribute, new ConfigVarSharedStaticRectContainer(sharedStatic))
        {
        }
    }
}
