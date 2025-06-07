using System;
using CodeGenHelpers.Internals;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0008
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public class EventBuilder : BuilderBase<EventBuilder>
    {
        private Accessibility _declaredAccessibility = Accessibility.Public;
        private string? _explicitImplementation = null;
        private string _eventDelegateType = nameof(EventHandler);
        private Action<ICodeWriter>? _addHandlerDelegate = null;
        private Action<ICodeWriter>? _removeHandlerDelegate = null;
        private bool _static = false;

        internal EventBuilder(ClassBuilder @class, string eventName)
        {
            Name = eventName;
            Class = @class;
        }

        public string Name { get; }

        public ClassBuilder Class { get; }

        public EventBuilder MakeStatic(bool isStatic = true)
        {
            _static = isStatic;
            return this;
        }

        public ClassBuilder WithBackingField(string backingField)
        {
            return WithAddExpression($"{backingField} += value")
                .WithRemoveExpression($"{backingField} -= value")
                .Class;
        }

        public EventBuilder WithAddHandler(Action<ICodeWriter> addDelegate)
        {
            void BlockHandler(ICodeWriter writer)
            {
                using (writer.Block("add"))
                    addDelegate?.Invoke(writer);
            }

            _addHandlerDelegate = BlockHandler;
            return this;
        }

        public EventBuilder WithAddExpression(string addDelegateExpression)
        {
            _addHandlerDelegate = w => w.AppendLine($"add => {addDelegateExpression};");
            return this;
        }

        public EventBuilder WithRemoveHandler(Action<ICodeWriter> addDelegate)
        {
            void BlockHandler(ICodeWriter writer)
            {
                using (writer.Block("remove"))
                    addDelegate?.Invoke(writer);
            }

            _removeHandlerDelegate = BlockHandler;
            return this;
        }

        public EventBuilder WithRemoveExpression(string addDelegateExpression)
        {
            _removeHandlerDelegate = w => w.AppendLine($"remove => {addDelegateExpression};");
            return this;
        }

        public EventBuilder WithExplicitImplementation(string @inferfaceName)
        {
            _explicitImplementation = inferfaceName;
            return this;
        }

        public ClassBuilder WithDefaultExplicitImplementation(string @interfaceName, Accessibility backingFieldAccessibility = Accessibility.Private, string? backingFieldName = null)
        {
            if (backingFieldName is null || string.IsNullOrEmpty(backingFieldName))
                backingFieldName = $"_{char.ToLower(Name[0])}{Name.Substring(1)}";

            return WithExplicitImplementation(interfaceName)
                .WithBackingField(backingFieldName)
                .AddEvent(backingFieldName)
                .WithAccessibility(backingFieldAccessibility)
                .WithDelegateHandler(_eventDelegateType)
                .Class;
        }

        public EventBuilder WithDelegateHandler(string handlerType)
        {
            _eventDelegateType = handlerType;
            return this;
        }

        public EventBuilder WithDelegateHandler(INamedTypeSymbol handlerType)
        {
            _eventDelegateType = handlerType.Name;
            return AddNamespaceImport(handlerType);
        }

        public EventBuilder WithAccessibility(Accessibility accessibility)
        {
            _declaredAccessibility = accessibility;
            return this;
        }

        public EventBuilder MakePublic()
        {
            _declaredAccessibility = Accessibility.Public;
            return this;
        }

        public EventBuilder MakeProtected()
        {
            _declaredAccessibility = Accessibility.Protected;
            return this;
        }

        public EventBuilder MakePrivate()
        {
            _declaredAccessibility = Accessibility.Private;
            return this;
        }

        public EventBuilder MakeInternal()
        {
            _declaredAccessibility = Accessibility.Internal;
            return this;
        }

        public EventBuilder MakeInternalProtected()
        {
            _declaredAccessibility = Accessibility.ProtectedAndInternal;
            return this;
        }

        public override EventBuilder AddAssemblyAttribute(string attribute)
        {
            Class.AddAssemblyAttribute(attribute);
            return this;
        }

        public override EventBuilder AddNamespaceImport(string importedNamespace)
        {
            Class.AddNamespaceImport(importedNamespace);
            return this;
        }

        public override EventBuilder AddNamespaceImport(ISymbol symbol)
        {
            Class.AddNamespaceImport(symbol);
            return this;
        }

        public override EventBuilder AddNamespaceImport(INamespaceSymbol symbol)
        {
            Class.AddNamespaceImport(symbol);
            return this;
        }

        internal override void Write(in CodeWriter writer)
        {
            if (Warning is not null)
            {
                writer.AppendLine("#warning " + Warning);
            }

            var @static = _static ? "static " : string.Empty;
            var eventDeclaration = $"{AccessibilityHelpers.Code(_declaredAccessibility)} {@static}event {_eventDelegateType} {Name}";
            if(!string.IsNullOrEmpty(_explicitImplementation))
            {
                eventDeclaration = $"event {@static}{_eventDelegateType} {_explicitImplementation}.{Name}";
            }

            if(_addHandlerDelegate is null || _removeHandlerDelegate is null)
            {
                writer.AppendLine($"{eventDeclaration};");
                return;
            }

            using (writer.Block(eventDeclaration))
            {
                _addHandlerDelegate(writer);
                _removeHandlerDelegate(writer);
            }
        }
    }
}
