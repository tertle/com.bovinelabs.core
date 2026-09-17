namespace BovineLabs.Core.ConfigVars
{
    using System;

    public interface IConfigVarContainer
    {
        string StringValue { get; set; }

        Type Type { get; }
    }

    public interface IConfigVarContainer<T> : IConfigVarContainer
    {
        public T Value { get; set; }
    }

    public class NullConfigVarContainer : IConfigVarContainer
    {
        public string StringValue { get; set; } = string.Empty;

        public Type Type => typeof(Nullable);
    }
}
