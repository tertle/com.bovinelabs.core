namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using UnityEngine;

    public class ConfigVarSharedStaticColorContainer : IConfigVarContainer<Color>
    {
        private readonly SharedStatic<Color> _field;

        public ConfigVarSharedStaticColorContainer(SharedStatic<Color> field)
        {
            _field = field;
        }

        Color IConfigVarContainer<Color>.Value
        {
            get => _field.Data;
            set => _field.Data = value;
        }

        string IConfigVarContainer.StringValue
        {
            get => ConfigVarAttribute.RectToVector4(_field.Data);
            set => _field.Data = ConfigVarAttribute.StringToVector4(value);
        }

        public Type Type => typeof(Color);
    }
}
