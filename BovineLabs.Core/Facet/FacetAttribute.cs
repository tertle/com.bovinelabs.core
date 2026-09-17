namespace BovineLabs.Core
{
    using System;

    /// <summary> Marks a field so the facet generator treats it as an embedded facet. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class FacetAttribute : Attribute
    {
    }
}
