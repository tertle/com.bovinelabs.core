// <copyright file="EntityQueryBuilderExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using Unity.Collections;
    using Unity.Entities;

    public static class EntityQueryBuilderExtensions
    {
        public static EntityQueryBuilder WithAll(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType> { type };
            return entityQueryBuilder.WithAll(ref list);
        }

        public static EntityQueryBuilder WithAny(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType> { type };
            return entityQueryBuilder.WithAny(ref list);
        }

        public static EntityQueryBuilder WithNone(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType> { type };
            return entityQueryBuilder.WithNone(ref list);
        }
    }
}
