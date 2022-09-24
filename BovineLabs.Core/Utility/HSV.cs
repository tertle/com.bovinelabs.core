// <copyright file="HSV.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.Utility
{
    using Unity.Mathematics;
    using UnityEngine;

    public struct HSV
    {
        public float H { get; }

        public float S { get; }

        public float V { get; }

        public HSV(float h, float s, float v)
        {
            // TODO validate
            this.H = math.clamp(h, 0, 360);
            if (math.abs(this.H - 360) < float.Epsilon)
            {
                this.H = 0;
            }

            this.S = math.clamp(s, 0, 1);
            this.V = math.clamp(v, 0, 1);
        }

        public HSV(float h)
            : this(h, 1, 1)
        {
        }

        public Color ToColor()
        {
            var c = this.V * this.S;
            var hh = this.H / 60f;
            var x = c * (1 - math.abs((hh % 2) - 1));
            var m = this.V - c;

            if (this.H < 60)
                return new Color(c + m, x + m, m);
            if (this.H < 120)
                return new Color(x + m, c + m, m);
            if (this.H < 180)
                return new Color(m, c + m, x + m);
            if (this.H < 240)
                return new Color(m, x + m, c + m);
            return new Color(x + m, m, c + m);
        }
    }
}
