// <copyright file="SearchView.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#nullable disable
namespace BovineLabs.Core.Editor.SearchWindow
{
    using System;
    using System.Text;
    using UnityEngine;

    /// <summary> Data contract retained for existing BovineLabs search picker callers. </summary>
    public static class SearchView
    {
        public struct Item : IEquatable<Item>
        {
            public string Path;
            public Texture2D Icon;
            public object Data;

            public string Name
            {
                get
                {
                    var lastIndex = this.Path.LastIndexOf('/');
                    return lastIndex == -1 ? this.Path : this.Path.Substring(lastIndex + 1);
                }
            }

            /// <inheritdoc/>
            public bool Equals(Item other)
            {
                return this.Path == other.Path && Equals(this.Icon, other.Icon) && Equals(this.Data, other.Data);
            }

            /// <inheritdoc/>
            public override bool Equals(object obj)
            {
                return obj is Item other && this.Equals(other);
            }

            /// <inheritdoc/>
            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = this.Path != null ? this.Path.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (this.Icon != null ? this.Icon.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (this.Data != null ? this.Data.GetHashCode() : 0);
                    return hashCode;
                }
            }

            public static string ConvertTypeToPath(string typeName)
            {
                var result = new StringBuilder();
                var angleBracketDepth = 0;

                foreach (var character in typeName)
                {
                    if (character == '<')
                    {
                        angleBracketDepth++;
                        result.Append(character);
                    }
                    else if (character == '>')
                    {
                        angleBracketDepth--;
                        result.Append(character);
                    }
                    else if (character == '.' && angleBracketDepth == 0)
                    {
                        result.Append('/');
                    }
                    else
                    {
                        result.Append(character);
                    }
                }

                return result.ToString();
            }
        }
    }
}
