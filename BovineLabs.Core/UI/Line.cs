// <copyright file="Line.cs" company="BovineLabs">
//     Copyright (c) BovineLabs. All rights reserved.
// </copyright>

namespace BovineLabs.Core.UI
{
    using UnityEngine;
    using UnityEngine.UIElements;

    public class Line : VisualElement
    {
        private Vector2 from;
        private Color lineColor;
        private float lineWidth;
        private Vector2 to;

        public Line(Vector2 from, Vector2 to, Color lineColor, float lineWidth = 1f)
        {
            this.from = from;
            this.to = to;
            this.lineColor = lineColor;
            this.lineWidth = lineWidth;

            this.style.position = Position.Absolute;

            this.generateVisualContent += this.OnGenerateVisualContent;
        }

        public Line(Color lineColor, float lineWidth = 1f)
            : this(Vector2.zero, Vector2.zero, lineColor, lineWidth)
        {
        }

        public Vector2 From
        {
            get => this.from;
            set
            {
                this.from = value;
                this.MarkDirtyRepaint();
            }
        }

        public Vector2 To
        {
            get => this.to;
            set
            {
                this.to = value;
                this.MarkDirtyRepaint();
            }
        }

        public float Width
        {
            get => this.lineWidth;
            set
            {
                this.lineWidth = value;
                this.MarkDirtyRepaint();
            }
        }

        public Color Color
        {
            get => this.lineColor;
            set
            {
                this.lineColor = value;
                this.MarkDirtyRepaint();
            }
        }

        public static Vector2 PerpendicularClockwise(Vector2 vector2)
        {
            return new Vector2(vector2.y, -vector2.x);
        }

        public static Vector2 PerpendicularCounterClockwise(Vector2 vector2)
        {
            return new Vector2(-vector2.y, vector2.x);
        }

        private void OnGenerateVisualContent(MeshGenerationContext cxt)
        {
            const int verts = 4;

            var mesh = cxt.Allocate(verts, 6);

            var dir = this.To - this.From;

            var perpcw = ((Vector3)PerpendicularClockwise(dir).normalized * this.Width) / 2f;
            var perpccw = ((Vector3)PerpendicularCounterClockwise(dir).normalized * this.Width) / 2f;

            var vertices = new Vertex[verts];
            vertices[0].position = new Vector3(this.To.x, this.To.y, Vertex.nearZ) + perpccw;
            vertices[1].position = new Vector3(this.To.x, this.To.y, Vertex.nearZ) + perpcw;
            vertices[2].position = new Vector3(this.From.x, this.From.y, Vertex.nearZ) + perpcw;
            vertices[3].position = new Vector3(this.From.x, this.From.y, Vertex.nearZ) + perpccw;

            vertices[0].tint = this.lineColor;
            vertices[1].tint = this.lineColor;
            vertices[2].tint = this.lineColor;
            vertices[3].tint = this.lineColor;

            mesh.SetAllVertices(vertices);
            mesh.SetAllIndices(new ushort[] { 0, 2, 1, 0, 3, 2 });
        }
    }
}
