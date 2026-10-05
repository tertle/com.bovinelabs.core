namespace BovineLabs.Core.Utility
{
    using System;

    /// <summary>Declares this component as an override of the target component for query-group filtering.</summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class QueryGroupAttribute : Attribute
    {
        public QueryGroupAttribute(Type targetType)
        {
            TargetType = targetType ?? throw new ArgumentNullException(nameof(targetType));
        }

        public Type TargetType { get; }
    }
}
