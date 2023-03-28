﻿// <copyright file="ButtonState.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

#if !BL_DISABLE_INPUT
namespace BovineLabs.Core.Input
{
    public struct ButtonState
    {
        public bool Down;
        public bool Pressed;
        public bool Up;

        public void Started()
        {
            this.Down = true;
            this.Pressed = true;
        }

        public void Cancelled()
        {
            this.Pressed = false;
            this.Up = true;
        }

        public void ResetFrame()
        {
            this.Down = false;
            this.Up = false;
        }
    }
}
#endif
