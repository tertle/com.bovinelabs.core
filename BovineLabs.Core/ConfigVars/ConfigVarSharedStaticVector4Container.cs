namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using UnityEngine;

    public class ConfigVarSharedStaticVector4Container : IConfigVarContainer<Vector4>
    {
        private readonly SharedStatic<Vector4> _field;

        public ConfigVarSharedStaticVector4Container(SharedStatic<Vector4> field)
        {
            _field = field;
        }

        Vector4 IConfigVarContainer<Vector4>.Value
        {
            get => _field.Data;
            set => _field.Data = value;
        }

        string IConfigVarContainer.StringValue
        {
            get => ConfigVarAttribute.RectToVector4(_field.Data);
            set => _field.Data = ConfigVarAttribute.StringToVector4(value);
        }

        public Type Type => typeof(Vector4);
    }
}
