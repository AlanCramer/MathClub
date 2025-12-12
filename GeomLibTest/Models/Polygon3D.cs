using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Media.Media3D;

namespace GeomLibTest.Models
{
    public sealed class Polygon3D
    {
        public List<Point3D> Vertices { get; } = new();

        public Polygon3D() { }

        public Polygon3D(IEnumerable<Point3D> vertices)
        {
            if (vertices == null) throw new ArgumentNullException(nameof(vertices));
            Vertices.AddRange(vertices);
        }

        /// <summary>
        /// Samples points along each edge.
        /// nPerEdge = number of interior points between endpoints.
        /// closed=true includes last->first edge.
        /// </summary>
        public List<Point3D> Sample(int nPerEdge, bool closed = true)
        {
            if (nPerEdge < 0) throw new ArgumentOutOfRangeException(nameof(nPerEdge));
            if (Vertices.Count == 0) return new List<Point3D>();
            if (Vertices.Count == 1) return new List<Point3D> { Vertices[0] };
            if (Vertices.Count == 2) closed = false;

            int edgeCount = closed ? Vertices.Count : Vertices.Count - 1;
            if (edgeCount <= 0) return new List<Point3D>(Vertices);

            var outPts = new List<Point3D>(edgeCount * (nPerEdge + 1) + (closed ? 0 : 1));

            for (int i = 0; i < edgeCount; i++)
            {
                var a = Vertices[i];
                var b = Vertices[(i + 1) % Vertices.Count];

                outPts.Add(a);

                for (int k = 1; k <= nPerEdge; k++)
                {
                    double t = (double)k / (nPerEdge + 1);
                    outPts.Add(Lerp(a, b, t));
                }
            }

            if (!closed)
                outPts.Add(Vertices[^1]);

            Debug.Assert(outPts.Count > 0);
            return outPts;
        }

        private static Point3D Lerp(in Point3D a, in Point3D b, double t) =>
            new(a.X + (b.X - a.X) * t,
                a.Y + (b.Y - a.Y) * t,
                a.Z + (b.Z - a.Z) * t);
    }
}
