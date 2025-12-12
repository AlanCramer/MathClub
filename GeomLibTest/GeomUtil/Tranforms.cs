using System.Diagnostics;
using System.Windows.Media.Media3D;

namespace GeomLibTest.GeomUtil
{
    public static class Transforms
    {
        public static Point3D InvertPoint(Point3D p, Point3D center, double radius)
        {
            var v = p - center;
            double r2 = radius * radius;
            double d2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z;

            const double Eps = 1e-9;
            if (d2 < Eps)
            {
                Debug.WriteLine("[WARN] Inversion of center is undefined; returning far point.");
                return new Point3D(center.X + 1000, center.Y, center.Z);
            }

            double scale = r2 / d2;
            return center + scale * v;
        }
    }
}
