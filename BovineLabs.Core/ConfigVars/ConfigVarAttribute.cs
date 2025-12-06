// <copyright file="ConfigVarAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using System.Globalization;
    using Unity.Burst;
    using UnityEngine;

    /// <summary> The attribute defining a config variable. This should only be placed on a <see cref="SharedStatic{T}" />. </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigVarAttribute : Attribute, IEquatable<ConfigVarAttribute>
    {
        /// <summary> Initializes a new instance of the <see cref="ConfigVarAttribute" /> class. </summary>
        /// <param name="name"> The name and key of the variable. </param>
        /// <param name="defaultValue"> The default value. </param>
        /// <param name="description"> A description of the variable. </param>
        /// <param name="isReadOnly"> Is the variable readonly. </param>
        /// <param name="isHidden"> Is the variable hidden from the config var window. </param>
        public ConfigVarAttribute(string name, string defaultValue, string description, bool isReadOnly = false, bool isHidden = false)
        {
            this.Name = name;
            this.Description = description;
            this.DefaultValue = defaultValue;
            this.IsReadOnly = isReadOnly;
            this.IsHidden = isHidden;
        }

        /// <summary> Initializes a new instance of the <see cref="ConfigVarAttribute" /> class. </summary>
        /// <param name="name"> The name and key of the variable. </param>
        /// <param name="defaultValue"> The default value. </param>
        /// <param name="description"> A description of the variable. </param>
        /// <param name="isReadOnly"> Is the variable readonly. </param>
        /// <param name="isHidden"> Is the variable hidden from the config var window. </param>
        public ConfigVarAttribute(string name, float defaultValue, string description, bool isReadOnly = false, bool isHidden = false)
            : this(name, defaultValue.ToString(CultureInfo.InvariantCulture), description, isReadOnly, isHidden)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="ConfigVarAttribute" /> class. </summary>
        /// <param name="name"> The name and key of the variable. </param>
        /// <param name="defaultValue"> The default value. </param>
        /// <param name="description"> A description of the variable. </param>
        /// <param name="isReadOnly"> Is the variable readonly. </param>
        /// <param name="isHidden"> Is the variable hidden from the config var window. </param>
        public ConfigVarAttribute(string name, int defaultValue, string description, bool isReadOnly = false, bool isHidden = false)
            : this(name, defaultValue.ToString(CultureInfo.InvariantCulture), description, isReadOnly, isHidden)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="ConfigVarAttribute" /> class. </summary>
        /// <param name="name"> The name and key of the variable. </param>
        /// <param name="defaultValue"> The default value. </param>
        /// <param name="description"> A description of the variable. </param>
        /// <param name="isReadOnly"> Is the variable readonly. </param>
        /// <param name="isHidden"> Is the variable hidden from the config var window. </param>
        public ConfigVarAttribute(string name, bool defaultValue, string description, bool isReadOnly = false, bool isHidden = false)
            : this(name, defaultValue.ToString(CultureInfo.InvariantCulture), description, isReadOnly, isHidden)
        {
        }

        /// <summary> Initializes a new instance of the <see cref="ConfigVarAttribute" /> class. </summary>
        /// <param name="name"> The name and key of the variable. </param>
        /// <param name="x"> The default x value. </param>
        /// <param name="y"> The default y value. </param>
        /// <param name="z"> The default z value. </param>
        /// <param name="w"> The default w value. </param>
        /// <param name="description"> A description of the variable. </param>
        /// <param name="isReadOnly"> Is the variable readonly. </param>
        /// <param name="isHidden"> Is the variable hidden from the config var window. </param>
        public ConfigVarAttribute(string name, float x, float y, float z, float w, string description, bool isReadOnly = false, bool isHidden = false)
            : this(name, RectToVector4(new Vector4(x, y, z, w)), description, isReadOnly, isHidden)
        {
        }

        /// <summary> Gets the name of the config var. </summary>
        public string Name { get; }

        /// <summary> Gets the description of the config var. </summary>
        public string Description { get; }

        /// <summary> Gets the default value of the config var. </summary>
        public string DefaultValue { get; }

        /// <summary> Gets a value indicating whether the config var read only. </summary>
        public bool IsReadOnly { get; }

        /// <summary> Gets a value indicating whether the config var is hidden in the editor window. </summary>
        public bool IsHidden { get; }

        public static implicit operator ConfigVarAttribute(string s)
        {
            return new ConfigVarAttribute(s, 0, string.Empty);
        }

        /// <inheritdoc />
        public bool Equals(ConfigVarAttribute other)
        {
            if (ReferenceEquals(null, other))
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return base.Equals(other) && this.Name == other.Name;
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ this.Name.GetHashCode();
            }
        }

        public static string RectToVector4(Vector4 v4)
        {
            return
                $"{v4.x.ToString(CultureInfo.InvariantCulture)}:{v4.y.ToString(CultureInfo.InvariantCulture)}:" +
                $"{v4.z.ToString(CultureInfo.InvariantCulture)}:{v4.w.ToString(CultureInfo.InvariantCulture)}";
        }

        public static Vector4 StringToVector4(string s)
        {
            var parts = s.Split(':');
            if (parts.Length != 4)
            {
                return default;
            }

            float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out var r);
            float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out var g);
            float.TryParse(parts[2], NumberStyles.Any, CultureInfo.InvariantCulture, out var b);
            float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out var a);

            return new Vector4(r, g, b, a);
        }
    }
}
