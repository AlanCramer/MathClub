using System;
using System.Diagnostics;

public readonly struct Vector2D
{
    public readonly double X;
    public readonly double Y;
    const double Epsilon = 1e-12;

    public Vector2D(double x, double y) { X = x; Y = y; }

    public double Length => Math.Sqrt(X * X + Y * Y);
    public double LengthSq => X * X + Y * Y;

    public Vector2D Normalized()
    {
        var len = Length;
        if (len < Epsilon) return new Vector2D(0, 0);
        return (1/len) * this;
    }

    public static double Dot(Vector2D a, Vector2D b) => a.X * b.X + a.Y * b.Y;

    /// Reflect vector v about unit normal n (n must be normalized!)
    public static Vector2D Reflect(Vector2D v, Vector2D nUnit)
    {
        // v' = v - 2 (v·n) n
        var vn = Dot(v, nUnit);
        return v - (2.0 * vn) * nUnit;
    }

    // --- operators ---
    public static Vector2D operator +(Vector2D a, Vector2D b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2D operator -(Vector2D a, Vector2D b) => new(a.X - b.X, a.Y - b.Y);
    public static Vector2D operator -(Vector2D v) => new(-v.X, -v.Y);

    public static Vector2D operator *(Vector2D v, double s) => new(v.X * s, v.Y * s);
    public static Vector2D operator *(double s, Vector2D v) => v * s;

    public override string ToString() => $"({X:0.###}, {Y:0.###})";

}
