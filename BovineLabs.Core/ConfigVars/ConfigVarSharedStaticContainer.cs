namespace BovineLabs.Core.ConfigVars
{
    using System;
    using Unity.Burst;

    internal class ConfigVarSharedStaticContainer<T> : IConfigVarContainer<T>
        where T : unmanaged
    {
        private readonly SharedStatic<T> field;

        public ConfigVarSharedStaticContainer(SharedStatic<T> field)
        {
            this.field = field;
        }

        T IConfigVarContainer<T>.Value
        {
            get => this.field.Data;
            set => this.field.Data = value;
        }

        string IConfigVarContainer.StringValue
        {
            get => this.field.Data.ToString();
            set => this.field.Data = (T)Convert.ChangeType(value, typeof(T));
        }

        public Type Type => typeof(T);
    }
}
