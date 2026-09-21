namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;
    using UnityEngine;

    public class ConfigVarSharedStaticRectContainer : IConfigVarContainer<Rect>
    {
        private readonly SharedStatic<Rect> _field;

        public ConfigVarSharedStaticRectContainer(SharedStatic<Rect> field)
        {
            _field = field;
        }

        Rect IConfigVarContainer<Rect>.Value
        {
            get => _field.Data;
            set => _field.Data = value;
        }

        string IConfigVarContainer.StringValue
        {
            get => ConfigVarAttribute.RectToVector4(RectToVector4(_field.Data));
            set => _field.Data = Vector4ToRect(ConfigVarAttribute.StringToVector4(value));
        }

        public Type Type => typeof(Vector4);

        private static Vector4 RectToVector4(Rect v4)
        {
            return new Vector4(v4.x, v4.y, v4.width, v4.height);
        }

        private static Rect Vector4ToRect(Vector4 v4)
        {
            return new Rect(v4.x, v4.y, v4.z, v4.w);
        }
    }
}
