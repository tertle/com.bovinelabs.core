// <copyright file="ObjectCategoriesAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_OBJECT_DEFINITION
namespace BovineLabs.Core.ObjectManagement
{
    using BovineLabs.Core.Keys;

    /// <summary> An attribute to mark a byte field is an object category to provide a convenient drop down drawer. </summary>
    public class ObjectCategoriesAttribute : KAttribute
    {
        public ObjectCategoriesAttribute(bool flag = true)
            : base(nameof(ObjectCategories), flag)
        {
        }
    }
}
#endif
