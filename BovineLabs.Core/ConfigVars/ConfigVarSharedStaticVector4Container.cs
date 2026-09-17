namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using UnityEngine;

    public class ConfigVarSharedStaticVector4Container : IConfigVarContainer<Vector4>
    {
        private readonly SharedStatic<Vector4> field;

        public ConfigVarSharedStaticVector4Container(SharedStatic<Vector4> field)
        {
            this.field = field;
        }

        Vector4 IConfigVarContainer<Vector4>.Value
        {
            get => this.field.Data;
            set => this.field.Data = value;
        }

        string IConfigVarContainer.StringValue
        {
            get => ConfigVarAttribute.RectToVector4(this.field.Data);
            set => this.field.Data = ConfigVarAttribute.StringToVector4(value);
        }

        public Type Type => typeof(Vector4);
    }
}
