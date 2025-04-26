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
        /// <param name="priority"> Negative priority means element will be to the left of things, positive to the right. </param>
        public EditorToolbarAttribute(EditorToolbarPosition position, int priority = 0)
        {
            this.Position = position;
            this.Priority = priority;
        }

        public EditorToolbarPosition Position { get; }

        public int Priority { get; }
    }
}
