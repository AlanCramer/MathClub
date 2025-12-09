using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HelixToolkit.Wpf;

namespace TransformPlaneDemo
{
    public partial class MainWindow : Window
    {
        // Visuals
        private LinesVisual3D _axes;
        private PointsVisual3D _points;

        // Backing data: each point has a start and end (x, y)
        private readonly List<(double xStart, double yStart, double xEnd, double yEnd)> _pointData = new();

        // Animation state
        private readonly Stopwatch _stopwatch = new();
        private bool _pointsCreated = false;
        private bool _isAnimating = false;
        private const double AnimationDurationSeconds = 2.0;

        // Domain and resolution for the plane
        private const int NumPointsX = 21; // number of samples in x
        private const int NumPointsY = 21; // number of samples in y
        private const double XMin = -5.0;
        private const double XMax = 5.0;
        private const double YMin = -5.0;
        private const double YMax = 5.0;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                InitializeScene();
                HookInput();
            };
        }

        private void InitializeScene()
        {
            View.Children.Clear();

            // Camera: orthographic, looking straight at XY plane
            if (View.Camera is ProjectionCamera cam)
            {
                cam.Position = new Point3D(0, 0, 20);
                cam.LookDirection = new Vector3D(0, 0, -20);
                cam.UpDirection = new Vector3D(0, 1, 0);
            }

            // Grey axes spanning slightly beyond our domain
            _axes = CreateAxes(-6, 6, -6, 6);
            View.Children.Add(_axes);

            // Prepare the PointsVisual3D but don't populate yet
            _points = new PointsVisual3D
            {
                Color = Colors.Red,
                Size = 4,
                Points = new Point3DCollection()
            };
            View.Children.Add(_points);
        }

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

        private void HookInput()
        {
            // Left-click:
            //  - first click: create the grid in the plane
            //  - second click: animate from (x,y) to transform(x,y)
            View.MouseLeftButtonDown += (_, __) => OnViewportClick();
        }

        private void OnViewportClick()
        {
            if (!_pointsCreated)
            {
                CreatePlaneSamplePoints();
                _pointsCreated = true;
                Debug.WriteLine("[TransformPlaneDemo] Plane points created.");
            }
            else if (!_isAnimating)
            {
                StartAnimation();
                Debug.WriteLine("[TransformPlaneDemo] Animation started.");
            }
        }

        private void CreatePlaneSamplePoints()
        {
            _pointData.Clear();

            var pts = new Point3DCollection();

            for (int iy = 0; iy < NumPointsY; iy++)
            {
                double ty = (double)iy / (NumPointsY - 1);
                double y = YMin + ty * (YMax - YMin);

                for (int ix = 0; ix < NumPointsX; ix++)
                {
                    double tx = (double)ix / (NumPointsX - 1);
                    double x = XMin + tx * (XMax - XMin);

                    // Start is the original position
                    double xStart = x;
                    double yStart = y;

                    // End is the transformed position
                    (double xEnd, double yEnd) = TransformExample(x, y);

                    _pointData.Add((xStart, yStart, xEnd, yEnd));
                    pts.Add(new Point3D(xStart, yStart, 0));
                }
            }

            _points.Points = pts;
        }

        private void StartAnimation()
        {
            if (_pointData.Count == 0)
                return;

            _stopwatch.Reset();
            _stopwatch.Start();
            _isAnimating = true;

            CompositionTarget.Rendering -= OnRenderFrame;
            CompositionTarget.Rendering += OnRenderFrame;
        }

        private void OnRenderFrame(object? sender, EventArgs e)
        {
            if (!_isAnimating)
                return;

            double elapsed = _stopwatch.Elapsed.TotalSeconds;
            double t = elapsed / AnimationDurationSeconds;

            if (t >= 1.0)
            {
                t = 1.0;
                _isAnimating = false;
                _stopwatch.Stop();
                CompositionTarget.Rendering -= OnRenderFrame;
                Debug.WriteLine("[TransformPlaneDemo] Animation completed.");
            }

            double eased = EaseInOutQuad(t);

            var pts = new Point3DCollection();
            foreach (var (xStart, yStart, xEnd, yEnd) in _pointData)
            {
                double x = Lerp(xStart, xEnd, eased);
                double y = Lerp(yStart, yEnd, eased);
                pts.Add(new Point3D(x, y, 0));
            }

            _points.Points = pts;
        }

        private static double Lerp(double a, double b, double t)
        {
            return a + (b - a) * t;
        }

        private static double EaseInOutQuad(double t)
        {
            if (t < 0.5) return 2 * t * t;
            t = 1 - t;
            return 1 - 2 * t * t;
        }

        /// <summary>
        /// Example 2D transformation (x, y) -> (x', y').
        /// You can plug in any transformation you like here:
        /// - linear matrix
        /// - shear
        /// - rotation
        /// - nonlinear warp, etc.
        /// </summary>
        private static (double x, double y) TransformExample(double x, double y)
        {
            double r = x * x + y * y;
            double warpStrength = 0.05; // tweak this
            double xWarp = warpStrength * x * r;
            double yWarp = warpStrength * y * r;

            return (xWarp, yWarp);

        }
    }
}
