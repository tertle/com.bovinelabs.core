namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;

    internal class ConfigVarSharedStaticContainer<T> : IConfigVarContainer<T>
        where T : unmanaged
    {
        private readonly SharedStatic<T> _field;

        public ConfigVarSharedStaticContainer(SharedStatic<T> field)
        {
            _field = field;
        }

        T IConfigVarContainer<T>.Value
        {
            get => _field.Data;
            set => _field.Data = value;
        }

        string IConfigVarContainer.StringValue
        {
            get => _field.Data.ToString();
            set => _field.Data = (T)Convert.ChangeType(value, typeof(T));
        }

        public Type Type => typeof(T);
    }
}
