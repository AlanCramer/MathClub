using System;
using System.Diagnostics;

public sealed class Ball
{
    public Point2D Position { get; private set; }
    public Vector2D Velocity { get; private set; }
    public Vector2D Acceleration { get; set; } = new Vector2D(0, 0);

    public double Radius { get; }
    public double Restitution { get; set; } = 1.0;

    public Ball(Point2D position, Vector2D velocity, double radius)
    {
        Position = position;
        Velocity = velocity;
        Radius = radius;
        DebugSanity();
    }

    public double Speed => Velocity.Length;

    public void SetState(Point2D position, Vector2D velocity)
    {
        Position = position;
        Velocity = velocity;
        DebugSanity();
    }

    public void Step(double dt)
    {
        if (dt <= 0) return;
        if (dt > 0.05) dt = 0.05;

        Velocity = Velocity + Acceleration * dt;
        Position = Position + Velocity * dt;

        DebugSanity();
    }

    public void Bounce(Vector2D normalUnit)
    {
        var nLen = normalUnit.Length;
        if (Math.Abs(nLen - 1.0) > 1e-3)
            Debug.WriteLine($"[WARN] normal not unit: |n|={nLen:0.000}");

        Velocity = Vector2D.Reflect(Velocity, normalUnit) * Restitution;
        DebugSanity();
    }

    public void Depenetrate(Vector2D normalUnit, double penetration)
    {
        if (penetration <= 0) return;
        Position = Position + normalUnit * (penetration + 1e-6);
    }

    private void DebugSanity()
    {
        if (!IsFinite(Position) || !IsFinite(Velocity) || !IsFinite(Acceleration))
            throw new InvalidOperationException(
                $"Ball state invalid. Pos={Position} Vel={Velocity} Acc={Acceleration}");
    }

    private static bool IsFinite(Point2D p) => double.IsFinite(p.X) && double.IsFinite(p.Y);
    private static bool IsFinite(Vector2D v) => double.IsFinite(v.X) && double.IsFinite(v.Y);
}
