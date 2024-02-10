// <copyright file="InputActionAttribute.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    using System;

    public class InputActionAttribute : Attribute
    {
        public InputActionAttribute(bool delta = false)
        {
            this.Delta = delta;
        }

        public bool Delta { get; set; }
    }

    public class InputActionDeltaAttribute : Attribute
    {
        public InputActionDeltaAttribute(bool delta = false)
        {
            this.Delta = delta;
        }

        public bool Delta { get; set; }
    }
}
#endif
