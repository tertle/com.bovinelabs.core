using System;
using Microsoft.CodeAnalysis;

#nullable enable
namespace CodeGenHelpers
{
    public abstract class BuilderBase<T> : BuilderBase
        where T : BuilderBase
    {
        public abstract T AddNamespaceImport(string importedNamespace);
        public abstract T AddNamespaceImport(ISymbol symbol);
        public abstract T AddNamespaceImport(INamespaceSymbol symbol);
        public abstract T AddAssemblyAttribute(string attribute);

        protected string? Warning;
        protected string? Condition;

        public T SetWarning(string warning)
        {
            Warning = warning;
            if (this is T thisAsT)
                return thisAsT;

            throw new InvalidOperationException($"The Builder must be of type {typeof(T).FullName}");
        }

        public T DisableWarning(string buildCode)
        {
            _pragmaWarnings.Add(buildCode);
            if (this is T thisAsT)
                return thisAsT;

            throw new InvalidOperationException($"The Builder must be of type {typeof(T).FullName}");
        }

        public T SetConditionalIf(string condition)
        {
            Condition = condition;
            if (this is T thisAsT)
                return thisAsT;

            throw new InvalidOperationException($"The Builder must be of type {typeof(T).FullName}");
        }
    }
}
