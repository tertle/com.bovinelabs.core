using System.Collections.Generic;
using System.Linq;
using CodeGenHelpers.Internals;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public class EnumBuilder : IBuilder
    {
        private readonly List<string> _attributes = new List<string>();
        private readonly List<EnumValueBuilder> _values = new List<EnumValueBuilder>();
        private DocumentationComment? _xmlDoc;

        internal EnumBuilder(string name, CodeBuilder builder)
        {
            Name = name;
            Builder = builder;
        }

        public CodeBuilder Builder { get; internal set; }

        public string Name { get; }

        public string FullyQualifiedName => $"{Builder.Namespace}.{Name}";

        public Accessibility? AccessModifier { get; private set; }

        public EnumBuilder WithSummary(string summary)
        {
            _xmlDoc = new SummaryDocumentationComment { Summary = summary };
            return this;
        }

        public EnumBuilder WithInheritDoc(bool inherit = true)
        {
            _xmlDoc = new InheritDocumentationComment();
            return this;
        }

        public EnumBuilder WithInheritDoc(string from)
        {
            _xmlDoc = new InheritDocumentationComment { InheritFrom = from };
            return this;
        }

        public EnumValueBuilder AddValue(string name, int? numericValue = null)
        {
            var builder = new EnumValueBuilder(name, this, numericValue);
            _values.Add(builder);
            return builder;
        }

        public EnumBuilder AddNamespaceImport(string importedNamespace)
        {
            Builder.AddNamespaceImport(importedNamespace);
            return this;
        }

        public EnumBuilder AddNamespaceImport(ISymbol symbol)
        {
            Builder.AddNamespaceImport(symbol);
            return this;
        }

        public EnumBuilder AddNamespaceImport(INamespaceSymbol symbol)
        {
            Builder.AddNamespaceImport(symbol);
            return this;
        }

        public EnumBuilder AddAttribute(string attribute)
        {
            var sanitized = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
            if (!_attributes.Contains(sanitized))
                _attributes.Add(sanitized);

            return this;
        }

        public EnumBuilder MakePublicEnum() => WithAccessModifier(Accessibility.Public);

        public EnumBuilder MakeInternalEnum() => WithAccessModifier(Accessibility.Internal);

        public EnumBuilder WithAccessModifier(Accessibility accessModifier)
        {
            AccessModifier = accessModifier;
            return this;
        }

        public string Build() => Builder.Build();

        void IBuilder.Write(in CodeWriter writer)
        {
            _xmlDoc?.Write(writer);

            var queue = new Queue<IBuilder>();
            _values.OrderBy(x => x.Value)
                //.ThenBy(x => x.Name)
                .ToList()
                .ForEach(x => queue.Enqueue(x));

            foreach(var attr in _attributes.OrderBy(x => x))
            {
                writer.AppendLine($"[{attr}]");
            }

            var parts = new[]
            {
                AccessibilityHelpers.Code(AccessModifier),
                "enum",
                Name
            };

            using (writer.Block(string.Join(" ", parts.Where(x => !string.IsNullOrEmpty(x)))))
            {
                while (queue.Any())
                {
                    var value = queue.Dequeue();
                    value.Write(writer);

                    if (queue.Any())
                    {
                        writer.AppendUnindentedLine(",");
                        writer.NewLine();
                    }
                    else
                    {
                        writer.NewLine();
                    }
                }
            }
        }
    }
}
