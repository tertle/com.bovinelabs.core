using System.Collections.Generic;
using System.Linq;

#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public class EnumValueBuilder : IBuilder
    {
        private readonly List<string> _attributes = new List<string>();
        private DocumentationComment? _xmlDoc;

        internal EnumValueBuilder(string name, EnumBuilder builder, int? value)
        {
            Name = name;
            Value = value;
            Enum = builder;
        }

        public string Name { get; }

        public int? Value { get; private set; }

        public EnumBuilder Enum { get; }

        public EnumValueBuilder WithSummary(string summary)
        {
            _xmlDoc = new SummaryDocumentationComment { Summary = summary };
            return this;
        }

        public EnumValueBuilder WithInheritDoc(bool inherit = true)
        {
            _xmlDoc = new InheritDocumentationComment();
            return this;
        }

        public EnumValueBuilder WithInheritDoc(string from)
        {
            _xmlDoc = new InheritDocumentationComment { InheritFrom = from };
            return this;
        }

        public EnumValueBuilder AddValue(string value, int? numericValue = null)
        {
            return Enum.AddValue(value, numericValue);
        }

        public EnumValueBuilder AddAttribute(string attribute)
        {
            var sanitized = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
            if (!_attributes.Contains(sanitized))
                _attributes.Add(sanitized);

            return this;
        }

        void IBuilder.Write(in CodeWriter writer)
        {
            _xmlDoc?.Write(writer);

            foreach (var attr in _attributes.OrderBy(x => x))
            {
                writer.AppendLine($"[{attr}]");
            }

            writer.Append(Name);
            if (Value.HasValue)
            {
                writer.AppendUnindented($" = {Value}");
            }
        }
    }
}
