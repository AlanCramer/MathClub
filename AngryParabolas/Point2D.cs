using System;

public readonly struct Point2D
{
    public readonly double X;
    public readonly double Y;

    public Point2D(double x, double y) { X = x; Y = y; }

    // point + vector = point
    public static Point2D operator +(Point2D p, Vector2D v) => new(p.X + v.X, p.Y + v.Y);

    // point - vector = point
    public static Point2D operator -(Point2D p, Vector2D v) => new(p.X - v.X, p.Y - v.Y);

    // point - point = vector
    public static Vector2D operator -(Point2D a, Point2D b) => new(a.X - b.X, a.Y - b.Y);

    public override string ToString() => $"({X:0.###}, {Y:0.###})";
}
