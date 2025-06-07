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
    public sealed class ConstructorBuilder : BuilderBase<ConstructorBuilder>, IParameterized<ConstructorBuilder>
    {
        private readonly List<ParameterBuilder<ConstructorBuilder>> _parameters = new List<ParameterBuilder<ConstructorBuilder>>();
        private readonly List<string> _attributes = new List<string>();
        private DocumentationComment? _xmlDoc;

        private Action<ICodeWriter>? _methodBodyWriter;

        private Func<string> _baseCall;

        internal ConstructorBuilder(Accessibility? accessModifier, ClassBuilder classBuilder)
        {
            AccessModifier = accessModifier;
            Class = classBuilder;
            _baseCall = () => string.Empty;
        }

        List<ParameterBuilder<ConstructorBuilder>> IParameterized<ConstructorBuilder>.Parameters => _parameters;

        ConstructorBuilder IParameterized<ConstructorBuilder>.Parent => this;

        public IReadOnlyCollection<ParameterBuilder<ConstructorBuilder>> Parameters => _parameters;

        public Accessibility? AccessModifier { get; }

        public ClassBuilder Class { get; }

        internal int Count => _parameters.Count;

        public ConstructorBuilder WithSummary(string summary)
        {
            if (_xmlDoc is not null && _xmlDoc is SummaryDocumentationComment summaryComment)
                summaryComment.Summary = summary;
            else
                _xmlDoc = new ParameterDocumentationComment { Summary = summary };

            return this;
        }

        public ConstructorBuilder WithInheritDoc(bool inherit = true)
        {
            _xmlDoc = new InheritDocumentationComment();
            return this;
        }

        public ConstructorBuilder WithInheritDoc(string from)
        {
            _xmlDoc = new InheritDocumentationComment { InheritFrom = from };
            return this;
        }

        public ConstructorBuilder WithParameterDoc(string paramName, string documentation)
        {
            if (_xmlDoc is not null && _xmlDoc is not ParameterDocumentationComment)
                throw new Exception("Documentation Comment has already been initialized using an InheritDoc.");

            var parameterDoc = _xmlDoc as ParameterDocumentationComment;
            if(parameterDoc is not null)
                parameterDoc.AddParameter(paramName, documentation);

            return this;
        }

        public override ConstructorBuilder AddAssemblyAttribute(string attribute)
        {
            Class.AddAssemblyAttribute(attribute);
            return this;
        }

        public ConstructorBuilder AddAttribute(string attribute)
        {
            var sanitized = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
            if (!_attributes.Contains(sanitized))
                _attributes.Add(sanitized);

            return this;
        }

        public ConstructorBuilder WithBody(Action<ICodeWriter> writerDelegate)
        {
            _methodBodyWriter = writerDelegate;
            return this;
        }

        public ConstructorBuilder WithThisCall()
        {
            _baseCall = () => ": this()";
            return this;
        }

        public ConstructorBuilder WithThisCall(Dictionary<string, string> parameters)
        {
            WithThisCall(parameters.Select(f => (f.Key, f.Value)));
            return this;
        }

        public ConstructorBuilder WithThisCall(IEnumerable<(string Type, string Name)> parameters)
        {
            foreach(var parameter in parameters)
            {
                this.AddParameter(parameter.Type, parameter.Name);
            }

            _baseCall = () =>
            {
                var output = parameters.Select(x => x.Name);
                return $": this({string.Join(", ", output)})";
            };
            return this;
        }

        public ConstructorBuilder WithThisCall(IEnumerable<IParameterSymbol> parameters)
        {
            var list = new List<(string Type, string Name)>();
            foreach (var parameter in parameters ?? Array.Empty<IParameterSymbol>())
            {
                AddNamespaceImport(parameter.Type);
                list.Add((parameter.Type.Name, parameter.Name));
            }
            return WithThisCall(list);
        }

        public ConstructorBuilder WithThisCall(IMethodSymbol baseConstructor)
        {
            return WithThisCall(baseConstructor.Parameters);
        }

        public ConstructorBuilder WithBaseCall()
        {
            _baseCall = () => ": base()";
            return this;
        }

        public ConstructorBuilder WithBaseCall(Dictionary<string, string> parameters)
        {
            WithBaseCall(parameters.Select(f => (f.Value, f.Key)));
            return this;
        }

        public ConstructorBuilder WithBaseCall(IEnumerable<(string Type, string Name)> parameters)
        {
            foreach (var parameter in parameters)
            {
                this.AddParameter(parameter.Type, parameter.Name);
            }

            _baseCall = () =>
            {
                var output = parameters.Select(x => x.Name);
                return $": base({string.Join(", ", output)})";
            };
            return this;
        }

        public ConstructorBuilder WithBaseCall(IEnumerable<IParameterSymbol> parameters)
        {
            var dict = new List<(string Type, string Name)>();
            foreach (var parameter in parameters ?? Array.Empty<IParameterSymbol>())
            {
                AddNamespaceImport(parameter.Type);
                dict.Add((parameter.Type.Name, parameter.Name));
            }
            return WithBaseCall(dict);
        }

        public ConstructorBuilder WithBaseCall(IMethodSymbol baseConstructor)
        {
            return WithBaseCall(baseConstructor.Parameters);
        }

        public override ConstructorBuilder AddNamespaceImport(string namespaceImport)
        {
            Class.AddNamespaceImport(namespaceImport);
            return this;
        }

        public override ConstructorBuilder AddNamespaceImport(ISymbol symbol)
        {
            return AddNamespaceImport(symbol.ContainingNamespace);
        }

        public override ConstructorBuilder AddNamespaceImport(INamespaceSymbol symbol)
        {
            return AddNamespaceImport(symbol.ToString());
        }

        public ConstructorBuilder AddConstructor(Accessibility? accessModifier = null)
        {
            return Class.AddConstructor(accessModifier);
        }

        internal override void Write(in CodeWriter writer)
        {
            if(_xmlDoc is ParameterDocumentationComment parameterDocumentation)
                parameterDocumentation.RemoveUnusedParameters(_parameters);

            _xmlDoc?.Write(writer);

            if (Warning is not null)
            {
                writer.AppendLine("#warning " + Warning);
            }

            foreach (var attribute in _attributes)
                writer.AppendLine($"[{attribute}]");

            var modifier = AccessModifier switch
            {
                null => Class.AccessModifier.ToString().ToLowerInvariant(),
                _ => AccessibilityHelpers.Code(AccessModifier)
            };
            var parameters = _parameters.Any() ? string.Join(", ", _parameters.Select(x => x.ToString())) : string.Empty;
            using(writer.Block($"{modifier} {Class.Name}({parameters})", _baseCall.Invoke()))
            {
                _methodBodyWriter?.Invoke(writer);
            }
        }
    }
}
