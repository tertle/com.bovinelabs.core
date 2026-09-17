namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using UnityEngine;

    public class ConfigVarSharedStaticColorContainer : IConfigVarContainer<Color>
    {
        private readonly SharedStatic<Color> field;

        public ConfigVarSharedStaticColorContainer(SharedStatic<Color> field)
        {
            this.field = field;
        }

        Color IConfigVarContainer<Color>.Value
        {
            get => this.field.Data;
            set => this.field.Data = value;
        }

        string IConfigVarContainer.StringValue
        {
            get => ConfigVarAttribute.RectToVector4(this.field.Data);
            set => this.field.Data = ConfigVarAttribute.StringToVector4(value);
        }

        public Type Type => typeof(Color);
    }
}
