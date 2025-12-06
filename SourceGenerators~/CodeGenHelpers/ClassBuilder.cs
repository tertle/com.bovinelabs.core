using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using CodeGenHelpers.Internals;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0008
#pragma warning disable IDE0079
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public sealed class ClassBuilder : BuilderBase<ClassBuilder>
    {
        private readonly List<string> _attributes = new();
        private readonly List<string> _interfaces = new();
        private readonly List<string> _classAttributes = new();
        private readonly List<ConstructorBuilder> _constructors = new();
        private readonly List<EventBuilder> _events = new();
        private readonly List<PropertyBuilder> _properties = new();
        private readonly List<MethodBuilder> _methods = new();
        private readonly Queue<ClassBuilder> _nestedClass = new();
        private readonly Queue<DelegateBuilder> _nestedDelegates = new();
        private readonly GenericCollection _generics = new();
        private readonly bool _isPartial;
        private readonly ClassBuilder? _parent;
        private DocumentationComment? _xmlDoc;
        private Func<PropertyBuilder, string>? _propertiesOrderBy = null;

        internal ClassBuilder(string className, CodeBuilder codeBuilder, bool partial = true, ClassBuilder? parent = null)
        {
            Name = className;
            Builder = codeBuilder;
            _isPartial = partial;
            _parent = parent;
        }

        public string Name { get; }

        public string FullyQualifiedName
        {
            get
            {
                var parts = new List<string>();
                var current = this;

                // Walk up the parent chain to build the full name
                while (current != null)
                {
                    parts.Insert(0, current.Name);
                    current = current._parent;
                }

                // Prepend the namespace if we have one
                if (Builder.Namespace is not null)
                {
                    parts.Insert(0, Builder.Namespace);
                }

                return string.Join(".", parts);
            }
        }

        public IReadOnlyList<ConstructorBuilder> Constructors => _constructors;

        public IReadOnlyList<PropertyBuilder> Properties => _properties;

        public IReadOnlyList<MethodBuilder> Methods => _methods;

        public IReadOnlyList<ClassBuilder> NestedClasses => _nestedClass.ToList();

        public CodeBuilder Builder { get; internal set; }

        public string? BaseClass { get; private set; }

        public Accessibility? AccessModifier { get; private set; }

        public TypeKind Kind { get; private set; } = TypeKind.Class;

        public bool IsStatic { get; private set; }

        public bool IsAbstract { get; private set; }

        public bool IsSealed { get; private set; }

        public bool IsReadOnly { get; private set; }

        public ClassBuilder WithSummary(string summary)
        {
            _xmlDoc = new SummaryDocumentationComment { Summary = summary };
            return this;
        }

        public ClassBuilder WithInheritDoc(bool inherit = true)
        {
            _xmlDoc = new InheritDocumentationComment();
            return this;
        }

        public ClassBuilder WithInheritDoc(string from)
        {
            _xmlDoc = new InheritDocumentationComment { InheritFrom = from };
            return this;
        }

        public ClassBuilder Sealed()
        {
            IsSealed = true;
            return this;
        }

        public ClassBuilder ReadOnly(bool isReadOnly = true)
        {
            IsReadOnly = isReadOnly;
            return this;
        }

        public ClassBuilder IsStruct()
        {
            Kind = TypeKind.Struct;
            return this;
        }

        public ClassBuilder OfType(TypeKind kind)
        {
            Kind = kind;
            return this;
        }

        public ClassBuilder SetBaseClass(string baseClass)
        {
            BaseClass = baseClass;

            if(BaseClass.Contains("."))
            {
                var className = baseClass.Split('.').Last();
                AddNamespaceImport(Regex.Replace(baseClass, @$"\.{className}", string.Empty));
            }

            return this;
        }

        public ClassBuilder SetBaseClass(INamedTypeSymbol symbol)
        {
            if(symbol.Name == Name)
            {
                BaseClass = $"global::{SymbolHelpers.GetFullMetadataName(symbol)}";
                return this;
            }

            BaseClass = symbol.Name;
            return AddNamespaceImport(symbol.ContainingNamespace);
        }

        public ClassBuilder AddGeneric(string name) =>
            AddGeneric(name, _ => { });

        public ClassBuilder AddGeneric(string name, Action<GenericBuilder> configureBuilder)
        {
            if(configureBuilder is null)
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

        public override ClassBuilder AddAssemblyAttribute(string attribute)
        {
            Builder.AddAssemblyAttribute(attribute);
            return this;
        }

        public ClassBuilder AddAttribute(string attribute)
        {
            var sanitized = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
            if (!_attributes.Contains(sanitized))
                _attributes.Add(sanitized);

            return this;
        }

        public override ClassBuilder AddNamespaceImport(string importedNamespace)
        {
            Builder.AddNamespaceImport(importedNamespace);
            return this;
        }

        public override ClassBuilder AddNamespaceImport(ISymbol symbol)
        {
            Builder.AddNamespaceImport(symbol);
            return this;
        }

        public override ClassBuilder AddNamespaceImport(INamespaceSymbol symbol)
        {
            Builder.AddNamespaceImport(symbol);
            return this;
        }

        public ClassBuilder AddInterface(string interfaceName)
        {
            _interfaces.Add(interfaceName);

            if (interfaceName.Contains('.'))
            {
                var name = interfaceName.Split('.').Last();
                return AddNamespaceImport(Regex.Replace(interfaceName, @$"\.{name}$", string.Empty));
            }

            return this;
        }

        public ClassBuilder AddInterface(ITypeSymbol symbol)
        {
            _interfaces.Add(symbol.Name);
            return AddNamespaceImport(symbol);
        }

        public ClassBuilder AddInterfaces(IEnumerable<string> interfaces)
        {
            if (interfaces is null)
                return this;

            foreach (var interfaceName in interfaces)
                AddInterface(interfaceName);

            return this;
        }

        public ClassBuilder AddInterfaces(IEnumerable<INamedTypeSymbol> interfaces)
        {
            if (interfaces is null)
                return this;

            foreach (var symbol in interfaces)
                AddInterface(symbol);

            return this;
        }

        public ConstructorBuilder AddConstructor(Accessibility? accessModifier = null)
        {
            var builder = new ConstructorBuilder(accessModifier, this);
            _constructors.Add(builder);
            return builder;
        }

        public ConstructorBuilder AddConstructor(IMethodSymbol baseConstructor, Accessibility? accessModifier = null)
        {
            var builder = AddConstructor(accessModifier)
                .AddParameters(baseConstructor.Parameters);
            _constructors.Add(builder);
            return builder;
        }

        public MethodBuilder AddMethod(string name, Accessibility? accessModifier = null)
        {
            var builder = new MethodBuilder(name, accessModifier, this);
            _methods.Add(builder);
            return builder;
        }

        public PropertyBuilder AddProperty(string name, Accessibility? accessModifier = null)
        {
            var builder = new PropertyBuilder(name, accessModifier, this);
            _properties.Add(builder);
            return builder;
        }

        public EventBuilder AddEvent(string eventName)
        {
            var builder = new EventBuilder(this, eventName);
            _events.Add(builder);
            return builder;
        }

        public ClassBuilder MakePublicClass() => WithAccessModifier(Accessibility.Public);

        public ClassBuilder MakeInternalClass() => WithAccessModifier(Accessibility.Internal);

        public ClassBuilder WithAccessModifier(Accessibility accessModifier)
        {
            AccessModifier = accessModifier;
            return this;
        }

        public ClassBuilder MakeStaticClass()
        {
            IsStatic = true;
            return this;
        }

        public ClassBuilder Abstract(bool isAbstract = true)
        {
            IsAbstract = isAbstract;
            return this;
        }

        public ClassBuilder AddNestedClass(string name, Accessibility? accessModifier = null) =>
            AddNestedClass(name, false, accessModifier);

        public ClassBuilder AddNestedClass(ITypeSymbol typeSymbol) =>
            AddNestedClass(typeSymbol.Name, true, typeSymbol.DeclaredAccessibility)
                .OfType(typeSymbol.TypeKind)
                .ReadOnly(typeSymbol.IsReadOnly);

        public ClassBuilder AddNestedClass(string name, bool partial, Accessibility? accessModifier = null)
        {
            var builder = new ClassBuilder(name, Builder, partial, this);
            if (accessModifier.HasValue)
                builder.WithAccessModifier(accessModifier.Value);

            _nestedClass.Enqueue(builder);
            return builder;
        }

        public DelegateBuilder AddNestedDelegate(string name, Accessibility? accessModifier = null)
        {
            var builder = new DelegateBuilder(name, Builder);
            if (accessModifier.HasValue)
                builder.WithAccessModifier(accessModifier.Value);

            _nestedDelegates.Enqueue(builder);
            return builder;
        }

        public string Build() => Builder.Build();

        public ClassBuilder DontSortPropertiesByName()
        {
            _propertiesOrderBy = _ => "";
            return this;
        }

        internal override void Write(in CodeWriter writer)
        {
            _xmlDoc?.Write(writer);

            if (Warning is not null)
            {
                writer.AppendLine("#warning " + Warning);
            }

            WriteClassAttributes(_classAttributes, writer);

            var staticDeclaration = IsStatic ? "static " : string.Empty;

            var queue = new Queue<string>();
            if (!string.IsNullOrEmpty(BaseClass))
            {
                queue.Enqueue(BaseClass!);
            }

            foreach (var inter in _interfaces.Where(x => !string.IsNullOrEmpty(x)).Distinct().OrderBy(x => x))
            {
                queue.Enqueue(inter);
            }

            var extra = queue.Any() ? $": {string.Join(", ", queue)}" : string.Empty;

            foreach (var attr in _attributes)
            {
                writer.AppendLine($"[{attr}]");
            }

            if (_methods.Any(x => x.IsAbstract))
                IsAbstract = true;

            var classDeclaration = new[] {
                AccessibilityHelpers.Code(AccessModifier),
                IsStatic ? "static" : null,
                IsSealed ? "sealed" : null,
                IsAbstract ? "abstract" : null,
                IsReadOnly && Kind == TypeKind.Struct ? "readonly" : null,
                _isPartial ? "partial" : null,
                (Kind == TypeKind.Struct ? "struct" : Kind.ToString()).ToLowerInvariant(), // Unity breaks here with Structure
                $"{Name}{_generics}",
                extra
            };

            using (writer.Block(string.Join(" ", classDeclaration.Where(x => !string.IsNullOrEmpty(x))), _generics.Contraints()))
            {
                var hadOutput = false;
                hadOutput = InvokeBuilderWrite(_nestedDelegates, ref hadOutput, in writer);
                hadOutput = InvokeBuilderWrite(_events, ref hadOutput, writer);
                var orderBy = _propertiesOrderBy ?? (x => x.Name);
                hadOutput = InvokeBuilderWrite(
                    _properties.Where(x => x.FieldTypeValue == PropertyBuilder.FieldType.Const && x.IsStatic == false)
                        .OrderBy(orderBy),
                    ref hadOutput,
                    writer,
                    true);
                hadOutput = InvokeBuilderWrite(
                    _properties.Where(x => x.FieldTypeValue == PropertyBuilder.FieldType.Const && x.IsStatic == true)
                        .OrderBy(orderBy),
                    ref hadOutput,
                    writer,
                    true);
                hadOutput = InvokeBuilderWrite(
                    _properties.Where(x => x.FieldTypeValue == PropertyBuilder.FieldType.ReadOnly)
                        .OrderBy(orderBy),
                    ref hadOutput,
                    writer,
                    true);
                hadOutput = InvokeBuilderWrite(
                    _properties.Where(x => x.FieldTypeValue == PropertyBuilder.FieldType.Default)
                        .OrderBy(orderBy),
                    ref hadOutput,
                    writer,
                    true);
                hadOutput = InvokeBuilderWrite(_constructors.OrderBy(x => x.Parameters.Count), ref hadOutput, writer);
                hadOutput = InvokeBuilderWrite(
                    _properties.Where(x => x.FieldTypeValue == PropertyBuilder.FieldType.Property)
                        .OrderBy(orderBy),
                    ref hadOutput,
                    writer);
                hadOutput = InvokeBuilderWrite(
                    _methods.OrderBy(x => x.Name)
                        .ThenBy(x => x.Parameters.Count),
                    ref hadOutput,
                    writer);
                InvokeBuilderWrite(_nestedClass, ref hadOutput, writer);
            }
        }

        private static void WriteClassAttributes(IEnumerable<string> classAttributes, in CodeWriter writer)
        {
            foreach (var attr in classAttributes.Distinct().OrderBy(x => x))
            {
                writer.AppendLine(attr);
            }
        }

        private static bool InvokeBuilderWrite<T>(IEnumerable<T> builders, ref bool hadOutput, in CodeWriter writer, bool group = false)
            where T : IBuilder
        {
            if (builders is null || !builders.Any())
                return hadOutput;

            if (hadOutput)
                writer.NewLine();

            var queue = new Queue<T>();
            foreach (var builder in builders)
                queue.Enqueue(builder);

            while (queue.Any())
            {
                var builder = queue.Dequeue();
                builder.Write(writer);

                if (!group && queue.Any())
                    writer.NewLine();
            }

            return true;
        }
    }
}
