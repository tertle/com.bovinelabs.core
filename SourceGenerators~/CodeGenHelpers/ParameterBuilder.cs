using Microsoft.CodeAnalysis;

#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public class ParameterBuilder<T>
        where T : BuilderBase<T>
    {
        internal ParameterBuilder(T parent)
        {
            Parent = parent;
        }

        public T Parent { get; }

        public string? Type { get; internal set; }
        public string? Name { get; internal set; }
        public string? DefaultValue { get; internal set; }
        public bool HasDefaultValue { get; internal set; }

        public ParameterBuilder<T> WithType(string typeName)
        {
            Type = typeName;
            return this;
        }

        public ParameterBuilder<T> WithType(INamedTypeSymbol symbol)
        {
            Parent.AddNamespaceImport(symbol);
            return WithType(symbol.Name);
        }

        public ParameterBuilder<T> WithDefaultValue() =>
            WithDefaultValue("default");

        public ParameterBuilder<T> WithDefaultValue(string defaultValue)
        {
            DefaultValue = defaultValue;
            HasDefaultValue = true;
            return this;
        }

        public ParameterBuilder<T> WithDefaultValue(object? value) =>
            WithDefaultValue($"{value}");

        public ParameterBuilder<T> WithNullDefault() =>
            WithDefaultValue("null");

        public override string ToString()
        {
            var output = $"{Type} {Name}";
            if (HasDefaultValue)
                output += $" = {DefaultValue}";

            return output;
        }

        public override bool Equals(object obj)
        {
            return obj != null && obj is ParameterBuilder<T> builder &&
                builder.Type == Type && builder.Name == Name;
        }

        public override int GetHashCode()
        {
            return ToString().GetHashCode();
        }
    }
}
