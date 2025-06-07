using System;
using System.Collections.Generic;
using System.Linq;
using CodeGenHelpers.Internals;
using Microsoft.CodeAnalysis;

namespace CodeGenHelpers
{
#nullable enable
    public sealed class DelegateBuilder : BuilderBase<DelegateBuilder>, IParameterized<DelegateBuilder>
    {
        private ParameterDocumentationComment? _xmlDoc;
        private readonly List<string> _attributes = new List<string>();
        private readonly GenericCollection _generics = new GenericCollection();
        private readonly List<ParameterBuilder<DelegateBuilder>> _parameters = new List<ParameterBuilder<DelegateBuilder>>();

        internal DelegateBuilder(string name, CodeBuilder builder)
        {
            Name = name;
            Builder = builder;
        }

        DelegateBuilder IParameterized<DelegateBuilder>.Parent => this;

        List<ParameterBuilder<DelegateBuilder>> IParameterized<DelegateBuilder>.Parameters => _parameters;

        public string Name { get; }

        public CodeBuilder Builder { get; internal set; }

        public Accessibility? AccessModifier { get; private set; }

        public string? ReturnType { get; private set; }

        public DelegateBuilder WithSummary(string summary)
        {
            if (_xmlDoc is null || _xmlDoc is not SummaryDocumentationComment summaryDoc)
                _xmlDoc = new ParameterDocumentationComment { Summary = summary };
            else
                summaryDoc.Summary = summary;

            return this;
        }

        public DelegateBuilder WithParameterDoc(string paramName, string documentation)
        {
            if (_xmlDoc is null)
                _xmlDoc = new ParameterDocumentationComment();

            if (_xmlDoc is not ParameterDocumentationComment parameterDoc)
                throw new Exception("DocumentationComment has already been initialized with a non ParameterDocumentationComment");

            parameterDoc.AddParameter(paramName, documentation);

            return this;
        }

        public DelegateBuilder AddGeneric(string name) =>
            AddGeneric(name, _ => { });

        public DelegateBuilder AddGeneric(string name, Action<GenericBuilder> configureBuilder)
        {
            if (configureBuilder is null)
                throw new ArgumentNullException(nameof(configureBuilder));
            else if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException(nameof(name));
            else if (_generics.Any(x => x.Name == name))
                throw new ArgumentException($"The argument {name} already exists");

            var builder = new GenericBuilder(name);
            configureBuilder(builder);
            _generics.Add(builder);
            return this;
        }

        public DelegateBuilder MakePublicDelegate() => WithAccessModifier(Accessibility.Public);

        public DelegateBuilder MakeInternalDelegate() => WithAccessModifier(Accessibility.Internal);

        public DelegateBuilder WithAccessModifier(Accessibility accessModifier)
        {
            AccessModifier = accessModifier;
            return this;
        }

        public DelegateBuilder AddAttribute(string attribute)
        {
            var sanitized = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
            if (!_attributes.Contains(sanitized))
                _attributes.Add(sanitized);

            return this;
        }

        public override DelegateBuilder AddAssemblyAttribute(string attribute)
        {
            Builder.AddAssemblyAttribute(attribute);
            return this;
        }

        public override DelegateBuilder AddNamespaceImport(string importedNamespace)
        {
            Builder.AddNamespaceImport(importedNamespace);
            return this;
        }

        public override DelegateBuilder AddNamespaceImport(ISymbol symbol)
        {
            Builder.AddNamespaceImport(symbol);
            return this;
        }

        public override DelegateBuilder AddNamespaceImport(INamespaceSymbol symbol)
        {
            Builder.AddNamespaceImport(symbol);
            return this;
        }

        public DelegateBuilder WithReturnType(string returnType)
        {
            ReturnType = returnType;
            return this;
        }

        internal override void Write(in CodeWriter writer)
        {
            if (_xmlDoc is ParameterDocumentationComment parameterDocumentation)
                parameterDocumentation.RemoveUnusedParameters(_parameters);

            _xmlDoc?.Write(writer);

            if (Warning is not null)
            {
                writer.AppendLine("#warning " + Warning);
            }

            foreach (var attribute in _attributes)
                writer.AppendLine($"[{attribute}]");

            var parameters = _parameters.Any() ? string.Join(", ", _parameters.Select(x => x.ToString())) : string.Empty;
            var parts = new string?[]
            {
                AccessibilityHelpers.Code(AccessModifier),
                "delegate",
                ReturnType is null || string.IsNullOrEmpty(ReturnType) ? "void" : ReturnType.Trim(),
                $"{Name}{_generics}({parameters})",
            }.Where(x => !string.IsNullOrEmpty(x));

            var output = string.Join(" ", parts);
            var constraints = _generics.Contraints();
            if(constraints.Any())
            {
                writer.AppendLine(output);
                writer.IncreaseIndent();
                for(int i = 0; i < constraints.Length; i++)
                {
                    var constraint = constraints[i];
                    if (i + 1 == constraints.Length)
                        constraint += ";";
                    writer.AppendLine(constraint);
                }
                writer.DecreaseIndent();
            }
            else
            {
                writer.AppendLine(output + ";");
            }
        }
    }
}
