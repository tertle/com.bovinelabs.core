using System;
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
    public class MethodBuilder : BuilderBase<MethodBuilder>, IBuilder, IParameterized<MethodBuilder>
    {
        private readonly List<string> _attributes = new List<string>();
        private readonly GenericCollection _generics = new GenericCollection();
        private DocumentationComment? _xmlDoc;
        private readonly List<ParameterBuilder<MethodBuilder>> _parameters = new List<ParameterBuilder<MethodBuilder>>();
        private bool _override;
        private bool _virtual;

        private Action<ICodeWriter>? _methodBodyWriter;

        internal MethodBuilder(string name, Accessibility? accessModifier, ClassBuilder builder)
        {
            Name = name;
            AccessModifier = accessModifier;
            Class = builder;
        }

        List<ParameterBuilder<MethodBuilder>> IParameterized<MethodBuilder>.Parameters => _parameters;
        MethodBuilder IParameterized<MethodBuilder>.Parent => this;

        public IReadOnlyCollection<ParameterBuilder<MethodBuilder>> Parameters => _parameters;

        public string Name { get; }

        public string? ReturnType { get; private set; }

        public bool IsAsync { get; private set; }

        public bool IsAbstract { get; private set; }

        public bool HasBody
        {
            get
            {
                if (_methodBodyWriter is null)
                    return false;

                var writer = new CodeWriter(IndentStyle.Spaces);
                _methodBodyWriter(writer);
                return !string.IsNullOrEmpty(writer.ToString());
            }
        }

        public ClassBuilder Class { get; }

        public Accessibility? AccessModifier { get; private set; }

        public bool IsStatic { get; private set; }

        public MethodBuilder WithSummary(string summary)
        {
            if (_xmlDoc is null || _xmlDoc is not SummaryDocumentationComment summaryDoc)
                _xmlDoc = new ParameterDocumentationComment { Summary = summary };
            else
                summaryDoc.Summary = summary;

            return this;
        }

        public MethodBuilder WithInheritDoc(bool inherit = true)
        {
            _xmlDoc = new InheritDocumentationComment();
            return this;
        }

        public MethodBuilder WithInheritDoc(string from)
        {
            _xmlDoc = new InheritDocumentationComment { InheritFrom = from };
            return this;
        }

        public MethodBuilder WithParameterDoc(string paramName, string documentation)
        {
            if (_xmlDoc is null)
                _xmlDoc = new ParameterDocumentationComment();

            if (_xmlDoc is not ParameterDocumentationComment parameterDoc)
                throw new Exception("DocumentationComment has already been initialized with a non ParameterDocumentationComment");

            parameterDoc.AddParameter(paramName, documentation);

            return this;
        }

        public MethodBuilder AddGeneric(string name) =>
            AddGeneric(name, _ => { });

        public MethodBuilder AddGeneric(string name, Action<GenericBuilder> configureBuilder)
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

        public override MethodBuilder AddNamespaceImport(string importedNamespace)
        {
            Class.AddNamespaceImport(importedNamespace);
            return this;
        }

        public override MethodBuilder AddNamespaceImport(ISymbol symbol)
        {
            return AddNamespaceImport(symbol.ContainingNamespace);
        }

        public override MethodBuilder AddNamespaceImport(INamespaceSymbol symbol)
        {
            return AddNamespaceImport(symbol.ToString());
        }

        public override MethodBuilder AddAssemblyAttribute(string attribute)
        {
            Class.AddAssemblyAttribute(attribute);
            return this;
        }

        public PropertyBuilder AddProperty(string name, Accessibility? accessModifier = null)
        {
            return Class.AddProperty(name, accessModifier);
        }

        public MethodBuilder MakeAsync()
        {
            IsAsync = true;
            return this;
        }

        public MethodBuilder Abstract(bool isAbstract = true)
        {
            IsAbstract = isAbstract;
            return this;
        }

        public MethodBuilder MakePublicMethod() => WithAccessModifier(Accessibility.Public);

        public MethodBuilder MakePrivateMethod() => WithAccessModifier(Accessibility.Private);

        public MethodBuilder MakeProtectedMethod() => WithAccessModifier(Accessibility.Protected);

        public MethodBuilder MakeInternalMethod() => WithAccessModifier(Accessibility.Internal);

        public MethodBuilder WithAccessModifier(Accessibility accessModifier)
        {
            AccessModifier = accessModifier;
            return this;
        }

        public MethodBuilder Override(bool @override = true)
        {
            _override = @override;
            return this;
        }

        public MethodBuilder MakeStaticMethod()
        {
            IsStatic = true;
            return this;
        }

        public MethodBuilder MakeVirtualMethod()
        {
            _virtual = true;
            return this;
        }

        public MethodBuilder AddAttribute(string attribute)
        {
            var sanitized = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
            if (!_attributes.Contains(sanitized))
                _attributes.Add(sanitized);

            return this;
        }

        public MethodBuilder WithBody(Action<ICodeWriter> writerDelegate)
        {
            _methodBodyWriter = writerDelegate;
            return this;
        }

        public MethodBuilder WithReturnType(string returnType)
        {
            ReturnType = returnType;
            return this;
        }

        internal override void Write(in CodeWriter writer)
        {
            if (Condition is not null)
            {
                writer.AppendUnindentedLine($"#if {this.Condition}");
            }

            if (Warning is not null)
            {
                writer.AppendLine("#warning " + Warning);
            }

            var output = ReturnType is null || string.IsNullOrEmpty(ReturnType) ? "void" : ReturnType.Trim();
            if (IsAsync)
                output = $"async {output}";

            if (_override)
                output = $"override {output}";
            else if (_virtual)
                output = $"virtual {output}";
            else if (IsAbstract)
                output = $"abstract {output}";
            else if (IsStatic)
                output = $"static {output}";

            var parameters = string.Join(", ", _parameters.Select(x => x.ToString()));
            output = $"{AccessibilityHelpers.Code(AccessModifier)} {output} {Name}{_generics}({parameters})";

            if(_xmlDoc is ParameterDocumentationComment parameterDocumentation)
                parameterDocumentation.RemoveUnusedParameters(_parameters);

            _xmlDoc?.Write(writer);

            foreach (var attribute in _attributes)
                writer.AppendLine($"[{attribute}]");

            if (IsAbstract)
            {
                writer.AppendLine($"{output.Trim()};");
                return;
            }

            using (writer.Block(output.Trim(), _generics.Contraints()))
            {
                _methodBodyWriter?.Invoke(writer);
            }

            if (Condition is not null)
            {
                writer.AppendUnindentedLine("#endif");
            }
        }
    }
}
