// <copyright file="EditorToolbarAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Editor.UI
{
    using System;
    using JetBrains.Annotations;

    [AttributeUsage(AttributeTargets.Method)]
    [MeansImplicitUse]
    public class EditorToolbarAttribute : Attribute
    {
        /// <summary> Initializes a new instance of the <see cref="EditorToolbarAttribute" /> class. </summary>
        /// <param name="position"> Where to position the button. </param>
        public EditorToolbarAttribute(EditorToolbarPosition position)
        {
            this.Position = position;
        }

        public EditorToolbarPosition Position { get; }
    }
}
