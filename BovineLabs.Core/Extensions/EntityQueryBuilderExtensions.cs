// <copyright file="EntityQueryBuilderExtensions.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Extensions
{
    using BovineLabs.Core.Utility;
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

            var list = new FixedList32Bytes<ComponentType>
            {
                new()
                {
                    TypeIndex = type.TypeIndex,
                    AccessModeType = ComponentType.AccessMode.ReadOnly,
                },
            };

            return entityQueryBuilder.WithAll(ref list);
        }

        public static EntityQueryBuilder WithAllRW(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType>
            {
                new()
                {
                    TypeIndex = type.TypeIndex,
                    AccessModeType = ComponentType.AccessMode.ReadWrite,
                },
            };

            return entityQueryBuilder.WithAll(ref list);
        }

        public static EntityQueryBuilder WithAny(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType>
            {
                new()
                {
                    TypeIndex = type.TypeIndex,
                    AccessModeType = ComponentType.AccessMode.ReadOnly,
                },
            };

            return entityQueryBuilder.WithAny(ref list);
        }

        public static EntityQueryBuilder WithAnyRW(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            if (type == default)
            {
                return entityQueryBuilder;
            }

            var list = new FixedList32Bytes<ComponentType>
            {
                new()
                {
                    TypeIndex = type.TypeIndex,
                    AccessModeType = ComponentType.AccessMode.ReadWrite,
                },
            };

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

        public static EntityQueryBuilder WithAnyWriteGroup<T>(this EntityQueryBuilder entityQueryBuilder)
        {
            return entityQueryBuilder.WithAnyWriteGroup(ComponentType.ReadOnly<T>());
        }

        public static EntityQueryBuilder WithAnyWriteGroup(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            var comps = TypeManagerUtil.GetWriteGroupComponents(type, Allocator.Temp);

            foreach (var c in comps)
            {
                entityQueryBuilder.WithAny(c);
            }

            return entityQueryBuilder;
        }

        public static EntityQueryBuilder WithNoneWriteGroup<T>(this EntityQueryBuilder entityQueryBuilder)
        {
            return entityQueryBuilder.WithNoneWriteGroup(ComponentType.ReadOnly<T>());
        }

        public static EntityQueryBuilder WithNoneWriteGroup(this EntityQueryBuilder entityQueryBuilder, ComponentType type)
        {
            var comps = TypeManagerUtil.GetWriteGroupComponents(type, Allocator.Temp);

            foreach (var c in comps)
            {
                entityQueryBuilder.WithNone(c);
            }

            return entityQueryBuilder;
        }
    }
}
