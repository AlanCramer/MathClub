using System;
using System.Collections.Generic;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace GeomLibTest.DrawUtil
{
    // Utilities for drawing static entities on the canvas, e.g. axes, grid, unit circle, etc.
    public static class Primitives
    {
        public static LinesVisual3D DrawCircle(Point3D center, double radius, int segments = 64)
        {
            if (segments < 3)
                throw new ArgumentException("segments must be at least 3.", nameof(segments));

            var lines = new LinesVisual3D();
            var pts = new List<Point3D>(segments + 1);

            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double angle = 2.0 * Math.PI * t;
                pts.Add(new Point3D(
                    center.X + radius * Math.Cos(angle),
                    center.Y + radius * Math.Sin(angle),
                    center.Z));
            }

            for (int i = 0; i < pts.Count - 1; i++)
            {
                lines.Points.Add(pts[i]);
                lines.Points.Add(pts[i + 1]);
            }

            return lines;
        }
    }
}
