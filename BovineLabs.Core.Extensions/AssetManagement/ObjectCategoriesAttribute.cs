// <copyright file="ObjectCategoriesAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.AssetManagement
{
    using BovineLabs.Core.Keys;

    /// <summary> An attribute to mark a byte field is an object category to provide a convenient drop down drawer. </summary>
    public class ObjectCategoriesAttribute : KAttribute
    {
        public ObjectCategoriesAttribute()
            : base(nameof(ObjectCategories))
        {
        }
    }
}
