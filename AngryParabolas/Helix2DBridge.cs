using System.Windows.Media.Media3D;

public static class Helix2DBridge
{
    public static Point2D ToP2(this Point3D p) => new(p.X, p.Y);
    public static Vector2D ToV2(this Vector3D v) => new(v.X, v.Y);

    public static Point3D ToP3(this Point2D p) => new(p.X, p.Y, 0);
}
