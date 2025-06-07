using System;
using System.Collections.Generic;
using CodeGenHelpers.Internals;
using Microsoft.CodeAnalysis;

#pragma warning disable IDE0008
#pragma warning disable IDE0090
#pragma warning disable IDE1006
#nullable enable
namespace CodeGenHelpers
{
    public class PropertyBuilder : IBuilder
    {
        internal enum FieldType
        {
            Const,
            ReadOnly,
            Default,
            Property,
            Ref,
            RefReadOnly,
        }

        private bool _autoprops;
        internal FieldType FieldTypeValue = FieldType.Property;
        internal ValueType PropertyValueType = ValueType.UserSpecified;
        private Action<ICodeWriter>? _getter;
        private string? _getterExpression;
        private Action<ICodeWriter>? _setter;
        private string? _setterExpression;
        private string? _value;
        private string? _warning;
        private bool _getOnly;
        private bool _virtual;
        private bool _override;
        private bool _ref;
        private bool _unsafe;
        private Accessibility? _setterAccessibility;
        private readonly List<string> _attributes = new List<string>();
        private DocumentationComment? _xmlDoc;

        internal PropertyBuilder(string name, Accessibility? accessModifier, ClassBuilder builder)
        {
            Name = name;
            AccessModifier = accessModifier;
            Class = builder;
        }

        public string Name { get; }

        public string? Type { get; private set; }

        public ClassBuilder Class { get; }

        public Accessibility? AccessModifier { get; private set; }

        public bool IsStatic { get; private set; }

        public bool IsRef { get; private set; }

        public PropertyBuilder WithSummary(string summary)
        {
            _xmlDoc = new SummaryDocumentationComment { Summary = summary };
            return this;
        }

        public PropertyBuilder WithInheritDoc(bool inherit = true)
        {
            _xmlDoc = new InheritDocumentationComment();
            return this;
        }

        public PropertyBuilder WithInheritDoc(string from)
        {
            _xmlDoc = new InheritDocumentationComment { InheritFrom = from };
            return this;
        }

        public PropertyBuilder AddNamespaceImport(string importedNamespace)
        {
            Class.AddNamespaceImport(importedNamespace);
            return this;
        }

        public PropertyBuilder AddNamespaceImport(ISymbol symbol)
        {
            return AddNamespaceImport(symbol.ContainingNamespace.ToString());
        }

        public PropertyBuilder AddNamespaceImport(INamespaceSymbol symbol)
        {
            return AddNamespaceImport(symbol.ToString());
        }

        public PropertyBuilder SetType(string type)
        {
            Type = type;
            return this;
        }

        public PropertyBuilder SetType(INamedTypeSymbol symbol)
        {
            return AddNamespaceImport(symbol.ContainingNamespace)
                .SetType(symbol.GetTypeName());
        }

        public PropertyBuilder SetType(Type type)
        {
            return AddNamespaceImport(type.Namespace)
                .SetType(type.GetTypeName());
        }

        public PropertyBuilder SetType<T>() => SetType(typeof(T));

        public PropertyBuilder SetWarning(string warning)
        {
            _warning = warning;
            return this;
        }

        public PropertyBuilder MakePublicProperty() => WithAccessModifier(Accessibility.Public);

        public PropertyBuilder MakePrivateProperty() => WithAccessModifier(Accessibility.Private);

        public PropertyBuilder MakeProtectedProperty() => WithAccessModifier(Accessibility.Protected);

        public PropertyBuilder MakeInternalProperty() => WithAccessModifier(Accessibility.Internal);

        public PropertyBuilder WithAccessModifier(Accessibility accessModifier)
        {
            AccessModifier = accessModifier;
            return this;
        }

        public PropertyBuilder Override(bool @override = true)
        {
            _override = @override;
            return this;
        }

        public PropertyBuilder Ref(bool @ref = true)
        {
            _ref = @ref;
            return this;
        }

        public PropertyBuilder Unsafe(bool @unsafe = true)
        {
            _unsafe = @unsafe;
            return this;
        }

        public PropertyBuilder MakeStatic()
        {
            IsStatic = true;
            if (string.IsNullOrEmpty(_getterExpression))
            {
                FieldTypeValue = FieldType.Const;
            }

            return this;
        }

        public PropertyBuilder MakeVirtualProperty()
        {
            _virtual = true;
            return this;
        }

        public PropertyBuilder AddAttribute(string attribute)
        {
            var sanitized = attribute.Replace("[", string.Empty).Replace("]", string.Empty);
            if (!_attributes.Contains(sanitized))
                _attributes.Add(sanitized);

            return this;
        }

        public ClassBuilder UseGetOnlyAutoProp()
        {
            _autoprops = true;
            _getOnly = true;
            return Class;
        }

        public ClassBuilder UseAutoProps() =>
            UseAutoProps(null);

        public ClassBuilder UseAutoProps(Accessibility? setterAccessibility)
        {
            _autoprops = true;
            _setterAccessibility = setterAccessibility;
            return Class;
        }

        public PropertyBuilder WithGetterExpression(string expression)
        {
            _getterExpression = expression;
            FieldTypeValue = FieldType.Property;
            return this;
        }

        public PropertyBuilder WithGetter(Action<ICodeWriter> getterBody)
        {
            _getter = getterBody;
            return this;
        }

        public PropertyBuilder WithSetterExpression(string expression)
        {
            _setterExpression = expression;
            return this;
        }

        public PropertyBuilder WithSetter(Action<ICodeWriter> setterBody)
        {
            _setter = setterBody;
            return this;
        }

        public ClassBuilder WithConstValue(string value, ValueType valueType = ValueType.UserSpecified)
        {
            _value = value;
            FieldTypeValue = FieldType.Const;
            PropertyValueType = valueType;
            return Class;
        }

        public ClassBuilder WithReadonlyValue(ValueType valueType = ValueType.UserSpecified) =>
            WithReadonlyValue(null, valueType);

        public ClassBuilder WithReadonlyValue(string? value, ValueType valueType = ValueType.UserSpecified)
        {
            _value = value;
            FieldTypeValue = FieldType.ReadOnly;
            PropertyValueType = valueType;
            return Class;
        }

        public ClassBuilder WithValue(string? value, ValueType valueType = ValueType.UserSpecified)
        {
            _value = value;
            FieldTypeValue = FieldType.Default;
            PropertyValueType = valueType;
            return Class;
        }

        public ClassBuilder MakeBackingField(string? defaultValue = null) =>
            WithValue(defaultValue);

        public PropertyBuilder WithBackingField(string? defaultValue = null, Accessibility? accessModifier = null)
        {
            var name = $"_{char.ToLower(Name[0])}{Name.Substring(1)}";
            var builder = Class.AddProperty(name, accessModifier ?? Accessibility.Private);
            builder.Type = Type;
            builder.FieldTypeValue = FieldType.Default;
            builder.WithValue(defaultValue);

            return this;
        }

        void IBuilder.Write(in CodeWriter writer)
        {
            _xmlDoc?.Write(writer);

            if (_warning is not null)
            {
                writer.AppendLine("#warning " + _warning);
            }

            foreach (var attribute in _attributes)
                writer.AppendLine($"[{attribute}]");

            var value = PropertyValueType switch
            {
                ValueType.Null => "null",
                ValueType.Default => "default",
                _ => _value
            };

            if (Type is null || string.IsNullOrEmpty(Type))
                throw new ArgumentNullException($"There is no 'Type' Specified for {Name}");

            var type = Type.Trim();
            var name = Name.Trim();
            var _static = IsStatic ? " static" : null;
            var isNew = name == nameof(Equals) ? " new" : string.Empty;
            string? additionalModifier = null;
            if (_virtual)
                additionalModifier = "virtual";
            else if (_override)
                additionalModifier = "override";

            if (_ref)
            {
                if (additionalModifier == null)
                {
                    additionalModifier = "ref";
                }
                else
                {
                    additionalModifier += " ref";
                }
            }

            if (_unsafe)
            {
                if (additionalModifier == null)
                {
                    additionalModifier = "unsafe";
                }
                else
                {
                    additionalModifier = "unsafe " + additionalModifier;
                }
            }

            var output = (FieldTypeValue switch
            {
                FieldType.Const => $"{AccessibilityHelpers.Code(AccessModifier)}{isNew} const {type} {name}",
                FieldType.ReadOnly => $"{AccessibilityHelpers.Code(AccessModifier)}{isNew}{_static} readonly {type} {name}",
                _ => additionalModifier is null
                    ? $"{AccessibilityHelpers.Code(AccessModifier)}{isNew}{_static} {type} {name}"
                    : $"{AccessibilityHelpers.Code(AccessModifier)} {additionalModifier} {type} {name}"
            }).Trim();

            if (FieldTypeValue == FieldType.Ref || FieldTypeValue == FieldType.RefReadOnly)
            {

            }
            if(FieldTypeValue != FieldType.Property)
            {
                if(string.IsNullOrEmpty(value) && FieldTypeValue != FieldType.Const)
                {
                    writer.AppendLine($"{output};");
                }
                else
                {
                    writer.AppendLine($"{output} = {value};");
                }

                return;
            }
            else if(_autoprops)
            {
                if (_getOnly)
                {
                    writer.AppendLine($"{output} {{ get; }}");
                    return;
                }

                var set = $"{AccessibilityHelpers.Code(_setterAccessibility)} set".Trim();
                writer.AppendLine($"{output} {{ get; {set}; }}");
                return;
            }

            if(!string.IsNullOrEmpty(_getterExpression) &&
                string.IsNullOrEmpty(_setterExpression) &&
                _setter is null)
            {
                writer.AppendLine($"{output} => {_getterExpression};");
                return;
            }

            if(string.IsNullOrEmpty(_getterExpression) && string.IsNullOrEmpty(_setterExpression) &&
                _getter is null && _setter is null)
            {
                writer.AppendLine($"{output};");
                return;
            }

            using (writer.Block(output))
            {
                if(!string.IsNullOrEmpty(_getterExpression))
                {
                    writer.AppendLine($"get => {_getterExpression};");
                }
                else if(_getter != null)
                {
                    using (writer.Block("get"))
                    {
                        _getter.Invoke(writer);
                    }
                }

                if (!string.IsNullOrEmpty(_setterExpression))
                {
                    writer.AppendLine($"set => {_setterExpression};");
                }
                else if (_setter != null)
                {
                    using (writer.Block("set"))
                    {
                        _setter.Invoke(writer);
                    }
                }
            }
        }
    }
}
