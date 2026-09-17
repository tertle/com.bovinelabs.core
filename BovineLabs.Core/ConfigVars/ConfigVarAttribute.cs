namespace BovineLabs.Core.ConfigVars
{
    using System;
    using System.Globalization;
    using Unity.Burst;
    using UnityEngine;

    /// <summary>
    /// Apply only to SharedStatic fields.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field)]
    public class ConfigVarAttribute : Attribute, IEquatable<ConfigVarAttribute>
    {
        public ConfigVarAttribute(string name, string defaultValue, string description, bool isReadOnly = false, bool isHidden = false)
        {
            this.Name = name;
            this.Description = description;
            this.DefaultValue = defaultValue;
            this.IsReadOnly = isReadOnly;
            this.IsHidden = isHidden;
        }

        public ConfigVarAttribute(string name, float defaultValue, string description, bool isReadOnly = false, bool isHidden = false)
            : this(name, defaultValue.ToString(CultureInfo.InvariantCulture), description, isReadOnly, isHidden)
        {
        }

        public ConfigVarAttribute(string name, int defaultValue, string description, bool isReadOnly = false, bool isHidden = false)
            : this(name, defaultValue.ToString(CultureInfo.InvariantCulture), description, isReadOnly, isHidden)
        {
        }

        public ConfigVarAttribute(string name, bool defaultValue, string description, bool isReadOnly = false, bool isHidden = false)
            : this(name, defaultValue.ToString(CultureInfo.InvariantCulture), description, isReadOnly, isHidden)
        {
        }

        public ConfigVarAttribute(string name, float x, float y, float z, float w, string description, bool isReadOnly = false, bool isHidden = false)
            : this(name, RectToVector4(new Vector4(x, y, z, w)), description, isReadOnly, isHidden)
        {
        }

        public string Name { get; }

        public string Description { get; }

        public string DefaultValue { get; }

        public bool IsReadOnly { get; }

        public bool IsHidden { get; }

        public static implicit operator ConfigVarAttribute(string s)
        {
            return new ConfigVarAttribute(s, 0, string.Empty);
        }

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
