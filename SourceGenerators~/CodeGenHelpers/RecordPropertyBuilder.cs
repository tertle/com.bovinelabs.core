using CodeGenHelpers.Internals;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public class RecordPropertyBuilder
    {
        public string Type { get; }
        public string Name { get; }
        public RecordBuilder Record { get; }
        public bool HasDefaultValue { get; private set; }
        public object? DefaultValue { get; private set; }
        public Accessibility? AccessModifier { get; private set; }

        public RecordPropertyBuilder(string type, string name, Accessibility accessModifier, RecordBuilder record)
        {
            Type = type;
            Name = name;
            AccessModifier = accessModifier;
            Record = record;
        }

        public override string ToString()
        {
            var output = $"{Type} {Name}";
            if (HasDefaultValue)
                output += $" = {DefaultValue}";

            return output;
        }

        public string ToPositionalProperty()
        {
            return ToString();
        }

        public string ToInitProperty()
        {
            string defaultValue = HasDefaultValue ? $" = {DefaultValue};" : string.Empty;
            return $"{AccessibilityHelpers.Code(AccessModifier)} {Type} {Name} {{ get; init; }}{defaultValue}";
        }

        public RecordPropertyBuilder WithDefaultValue(string defaultValue)
        {
            DefaultValue = defaultValue;
            HasDefaultValue = true;
            return this;
        }

        public RecordPropertyBuilder WithDefaultValue(object value) =>
            WithDefaultValue($"{value}");
    }
}
