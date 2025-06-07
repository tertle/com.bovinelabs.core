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
    public sealed class RecordBuilder : BuilderBase<RecordBuilder>
    {
        private readonly List<RecordPropertyBuilder> _properties = new List<RecordPropertyBuilder>();
        private readonly List<string> _attributes = new List<string>();
        private DocumentationComment? _xmlDoc;

        internal RecordBuilder(string name, CodeBuilder codeBuilder)
        {
            Name = name;
            Builder = codeBuilder;
        }

        public string Name { get; }

        public string FullyQualifiedName => $"{Builder.Namespace}.{Name}";

        public CodeBuilder Builder { get; internal set; }

        public Accessibility? AccessModifier { get; private set; }

        public RecordPropertyType PropertyType { get; private set; } = RecordPropertyType.Positional;

        public RecordBuilder WithSummary(string summary)
        {
            _xmlDoc = new SummaryDocumentationComment { Summary = summary };

            return this;
        }

        public RecordBuilder WithInheritDoc(bool inherit = true)
        {
            _xmlDoc = new InheritDocumentationComment();
            return this;
        }

        public RecordBuilder WithInheritDoc(string from)
        {
            _xmlDoc = new InheritDocumentationComment { InheritFrom = from };
            return this;
        }

        public RecordBuilder UsePositionalProperties()
        {
            PropertyType = RecordPropertyType.Positional;
            return this;
        }

        public RecordBuilder UseInitProperties()
        {
            PropertyType = RecordPropertyType.Init;
            return this;
        }

        public RecordPropertyBuilder AddProperty(string type,
            string name,
            Accessibility accessModifier = Accessibility.Public)
        {
            var prop = new RecordPropertyBuilder(type, name, accessModifier, this);
            _properties.Add(prop);
            return prop;
        }

        public RecordBuilder MakePublicRecord() => WithAccessModifier(Accessibility.Public);

        public RecordBuilder MakeInternalRecord() => WithAccessModifier(Accessibility.Internal);

        public RecordBuilder WithAccessModifier(Accessibility accessModifier)
        {
            AccessModifier = accessModifier;
            return this;
        }

        public string Build() => Builder.Build();

        internal override void Write(in CodeWriter writer)
        {
            _xmlDoc?.Write(writer);

            if (Warning is not null)
            {
                writer.AppendLine("#warning " + Warning);
            }

            foreach (var attribute in _attributes)
                writer.AppendLine($"[{attribute}]");

            if (PropertyType == RecordPropertyType.Init)
            {
                using (writer.Block($"{AccessibilityHelpers.Code(AccessModifier)} record {Name}"))
                {
                    foreach (RecordPropertyBuilder property in _properties)
                    {
                        writer.AppendLine(property.ToInitProperty());
                    }
                }

                return;
            }

            var properties = _properties.Any() ? string.Join(", ", _properties.Select(x => x.ToPositionalProperty())) : string.Empty;
            writer.AppendLine($"{AccessibilityHelpers.Code(AccessModifier)} record {Name}({properties});");
        }

        public override RecordBuilder AddNamespaceImport(string importedNamespace)
        {
            Builder.AddNamespaceImport(importedNamespace);
            return this;
        }

        public override RecordBuilder AddNamespaceImport(ISymbol symbol)
        {
            Builder.AddNamespaceImport(symbol);
            return this;
        }

        public override RecordBuilder AddNamespaceImport(INamespaceSymbol symbol)
        {
            Builder.AddNamespaceImport(symbol);
            return this;
        }

        public override RecordBuilder AddAssemblyAttribute(string attribute)
        {
            Builder.AddAssemblyAttribute(attribute);
            return this;
        }
    }
}
