namespace BovineLabs.Core.Editor.ConfigVars
{
    using System;
    using BovineLabs.Core.ConfigVars;
    using Unity.Burst;
    using UnityEngine.UIElements;

    internal class ConfigVarBinding<T> : ConfigVarBindingBase<T>
        where T : unmanaged, IEquatable<T>
    {
        public ConfigVarBinding(BaseField<T> baseField, ConfigVarAttribute attribute, SharedStatic<T> sharedStatic)
            : base(baseField, attribute, new ConfigVarSharedStaticContainer<T>(sharedStatic))
        {
        }
    }
}
