namespace BovineLabs.Core.Utility
{
    using Unity.Mathematics;
    using UnityEngine;

    public struct HSV
    {
        public float H { get; }

        public float S { get; }

        public float V { get; }

        public HSV(float h, float s = 1, float v = 1)
        {
            // TODO validate
            H = math.clamp(h, 0, 360);
            if (math.abs(H - 360) < math.EPSILON)
            {
                H = 0;
            }

            S = math.clamp(s, 0, 1);
            V = math.clamp(v, 0, 1);
        }

        public Color ToColor()
        {
            var c = V * S;
            var hh = H / 60f;
            var x = c * (1 - math.abs((hh % 2) - 1));
            var m = V - c;

            return H switch
            {
                < 60 => new Color(c + m, x + m, m),
                < 120 => new Color(x + m, c + m, m),
                < 180 => new Color(m, c + m, x + m),
                < 240 => new Color(m, x + m, c + m),
                _ => new Color(x + m, m, c + m),
            };
        }
    }
}
