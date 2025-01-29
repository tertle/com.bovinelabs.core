// <copyright file="StableTypeHashAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.PropertyDrawers
{
    using System;
    using UnityEngine;

    public class StableTypeHashAttribute : PropertyAttribute, IEquatable<StableTypeHashAttribute>
    {
        public StableTypeHashAttribute(TypeCategory category)
        {
            this.Category = category;
        }

        [Flags]
        public enum TypeCategory : byte
        {
            None = 0,

            /// <summary> Implements IComponentData (can be either a struct or a class). </summary>
            ComponentData = 1 << 0,

            /// <summary> Implements IBufferElementData (struct only). </summary>
            BufferData = 1 << 1,

            /// <summary> Implement ISharedComponentData (struct only). </summary>
            SharedComponentData = 1 << 2,
        }

        public TypeCategory Category { get; }

        public bool OnlyZeroSize { get; set; }

        public bool OnlySize { get; set; }

        public bool OnlyEnableable { get; set; }

        public bool AllowUnityNamespace { get; set; } = true;

        public bool AllowEditorAssemblies { get; set; }

        public Type[] BaseType { get; set; } = Type.EmptyTypes;

        public bool Equals(StableTypeHashAttribute other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return this.Category == other.Category &&
                this.OnlyZeroSize == other.OnlyZeroSize &&
                this.OnlyEnableable == other.OnlyEnableable &&
                this.AllowUnityNamespace == other.AllowUnityNamespace &&
                this.AllowEditorAssemblies == other.AllowEditorAssemblies;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)this.Category;
                hashCode = (hashCode * 397) ^ this.OnlyZeroSize.GetHashCode();
                hashCode = (hashCode * 397) ^ this.OnlyEnableable.GetHashCode();
                hashCode = (hashCode * 397) ^ this.AllowUnityNamespace.GetHashCode();
                hashCode = (hashCode * 397) ^ this.AllowEditorAssemblies.GetHashCode();
                return hashCode;
            }
        }
    }
}
