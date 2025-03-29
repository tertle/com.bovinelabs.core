// <copyright file="ConfigVarAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.ConfigVars
{
    using System;
    using System.Globalization;
    using Unity.Burst;

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
    }
}
