using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace CircleSineDemo
{
    public partial class MainWindow : Window
    {
        // Static visuals
        private LinesVisual3D _axes;
        private LinesVisual3D _unitCircle;

        // New: radial line from origin to circle point
        private LinesVisual3D _radiusLine;

        // Moving points
        private PointsVisual3D _circlePoint;
        private PointsVisual3D _sinePoint;  // on y-axis
        private PointsVisual3D _cosPoint;   // on x-axis

        // New: angle text label
        private BillboardTextVisual3D _angleLabel;

        // Animation state
        private readonly Stopwatch _stopwatch = new();
        private const double AngularSpeed = 1.0; // radians per second

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                InitializeScene();
                StartAnimationLoop();
            };
        }

        private void InitializeScene()
        {
            View.Children.Clear();

            // Camera: orthographic, looking straight at the XY plane
            if (View.Camera is ProjectionCamera cam)
            {
                cam.Position = new Point3D(0, 0, 10);
                cam.LookDirection = new Vector3D(0, 0, -10);
                cam.UpDirection = new Vector3D(0, 1, 0);
            }

            // Circle centered at origin
            var circleCenter = new Point3D(0, 0, 0);
            double circleRadius = 1.0;

            // 1) Axes (light gray)
            _axes = CreateAxes(-2, 2, -2, 2);
            View.Children.Add(_axes);

            // 2) Unit circle (wireframe)
            var circlePoints = SampleCircle(circleCenter, circleRadius, 128).ToList();
            _unitCircle = new LinesVisual3D
            {
                Points = ToSegmentPairs(circlePoints, closed: true),
                Color = Color.FromRgb(0, 0, 255), // blue
                Thickness = 1.5
            };
            View.Children.Add(_unitCircle);

            // 3) Radial line from origin to circle point
            _radiusLine = new LinesVisual3D
            {
                Points = new Point3DCollection
                {
                    new Point3D(0, 0, 0),
                    new Point3D(circleRadius, 0, 0) // initial at angle 0
                },
                Color = Colors.Red,
                Thickness = 1.5
            };
            View.Children.Add(_radiusLine);

            // 4) Moving point on unit circle: (cos t, sin t)
            _circlePoint = new PointsVisual3D
            {
                Color = Colors.Red,
                Size = 8,
                Points = new Point3DCollection
                {
                    new Point3D(circleCenter.X + circleRadius, circleCenter.Y, 0)
                }
            };
            View.Children.Add(_circlePoint);

            // 5) Sine marker on y-axis: (0, sin t)
            _sinePoint = new PointsVisual3D
            {
                Color = Colors.Green,
                Size = 8,
                Points = new Point3DCollection
                {
                    new Point3D(0, 0, 0)
                }
            };
            View.Children.Add(_sinePoint);

            // 6) Cosine marker on x-axis: (cos t, 0)
            _cosPoint = new PointsVisual3D
            {
                Color = Colors.Orange,
                Size = 8,
                Points = new Point3DCollection
                {
                    new Point3D(1, 0, 0)
                }
            };
            View.Children.Add(_cosPoint);

            // 7) Angle label (θ in radians)
            _angleLabel = new BillboardTextVisual3D
            {
                Position = new Point3D(1.3, 0.2, 0), // initial position
                Text = "θ = 0.00",
                FontSize = 16,
                Foreground = Brushes.Black
            };
            View.Children.Add(_angleLabel);
        }

        // Simple XY axes
        private LinesVisual3D CreateAxes(double minX, double maxX, double minY, double maxY)
        {
            var pts = new Point3DCollection
            {
                new Point3D(minX, 0, 0),
                new Point3D(maxX, 0, 0),
                new Point3D(0, minY, 0),
                new Point3D(0, maxY, 0)
            };

            return new LinesVisual3D
            {
                Points = pts,
                Color = Color.FromRgb(200, 200, 200),
                Thickness = 1
            };
        }

        // Sample a circle in the XY-plane
        private IEnumerable<Point3D> SampleCircle(Point3D center, double radius, int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                double t = 2 * Math.PI * i / steps;
                yield return new Point3D(
                    center.X + radius * Math.Cos(t),
                    center.Y + radius * Math.Sin(t),
                    center.Z);
            }
        }

        // Turn a vertex list into segment pairs for LinesVisual3D
        private Point3DCollection ToSegmentPairs(IList<Point3D> vertices, bool closed)
        {
            var collection = new Point3DCollection();

            int count = vertices.Count;
            if (count < 2)
                return collection;

            for (int i = 0; i < count - 1; i++)
            {
                collection.Add(vertices[i]);
                collection.Add(vertices[i + 1]);
            }

            if (closed)
            {
                collection.Add(vertices[count - 1]);
                collection.Add(vertices[0]);
            }

            return collection;
        }

        // --- Animation loop ---

        private void StartAnimationLoop()
        {
            _stopwatch.Start();
            CompositionTarget.Rendering += OnRenderFrame;
        }

        private void OnRenderFrame(object? sender, EventArgs e)
        {
            double t = _stopwatch.Elapsed.TotalSeconds * AngularSpeed;

            double radius = 1.0;

            double cosT = Math.Cos(t);
            double sinT = Math.Sin(t);

            // Normalize angle into [0, 2π) for display
            double theta = t % (2 * Math.PI);
            if (theta < 0) theta += 2 * Math.PI;

            // Circle point: (cos t, sin t)
            var circlePos = new Point3D(radius * cosT, radius * sinT, 0);
            _circlePoint.Points = new Point3DCollection { circlePos };

            // Radial line from origin to circle point
            _radiusLine.Points = new Point3DCollection
            {
                new Point3D(0, 0, 0),
                circlePos
            };

            // Sine marker on y-axis: (0, sin t)
            _sinePoint.Points = new Point3DCollection
            {
                new Point3D(0, sinT, 0)
            };

            // Cosine marker on x-axis: (cos t, 0)
            _cosPoint.Points = new Point3DCollection
            {
                new Point3D(cosT, 0, 0)
            };

            // Angle label: place slightly outside the circle along the radius
            double labelRadius = 1.3;
            var labelPos = new Point3D(labelRadius * cosT, labelRadius * sinT, 0);
            _angleLabel.Position = labelPos;
            _angleLabel.Text = $"θ = {theta:0.00}";
        }

    }
}
