using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.TextFormatting;
using System.Windows.Threading;
using HelixToolkit.Wpf;

namespace transformAxis
{
    public partial class MainWindow : Window
    {
        // Visuals
        private LinesVisual3D _axes;
        private PointsVisual3D _points;

        // Data backing the points: x, yStart, yEnd
        private readonly List<(double x, double yStart, double yEnd)> _pointData = new();

        // Animation state
        private readonly Stopwatch _stopwatch = new();
        private bool _pointsCreated = false;
        private bool _isAnimating = false;
        private const double AnimationDurationSeconds = 2.0;

        // Domain and scaling
        private const int NumPoints = 21;           // number of points along x
        private const double XMin = -10.0;
        private const double XMax = 10.0;
        // private const double FunctionScale = 0.1;  // scale x^2 so it fits in view

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

            // Grey axes spanning a bit beyond our domain and range
            _axes = CreateAxes(-11, 11, -1, 6);
            View.Children.Add(_axes);

            // Prepare the PointsVisual3D but don't populate yet
            _points = new PointsVisual3D
            {
                Color = Colors.Red,
                Size = 6,
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
            View.MouseLeftButtonDown += (_, __) => OnViewportClick();
        }

        private void OnViewportClick()
        {
            if (!_pointsCreated)
            {
                CreatePointsOnXAxis();
                _pointsCreated = true;
            }
            else if (!_isAnimating)
            {
                StartAnimation();
            }
        }

        private void CreatePointsOnXAxis()
        {
            _pointData.Clear();

            var pts = new Point3DCollection();

            for (int i = 0; i < NumPoints; i++)
            {
                double t = (double)i / (NumPoints - 1);
                double x = XMin + t * (XMax - XMin);

                double yStart = 0.0;
                double yEnd = transformSin(x); 

                _pointData.Add((x, yStart, yEnd));
                pts.Add(new Point3D(x, yStart, 0));     // initial positions on x-axis
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
                Debug.WriteLine("[FunctionLerpDemo] Animation completed.");
            }

            // Optional: simple ease-in-out (quadratic)
            double eased = EaseInOutQuad(t);

            var pts = new Point3DCollection();
            foreach (var (x, yStart, yEnd) in _pointData)
            {
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

        private static double transformXsquared(double x)
        {
            const double FunctionScale = 0.1;  // scale x^2 so it fits in view
            return FunctionScale * (x * x);
        }

        private static double transformSin(double x)
        {
            const double Frequency = 0.5; // controls how many oscillations
            const double Amplitude = 2.0; // controls height of oscillations
            return Amplitude * Math.Sin(Frequency * x);
        } 

    }
}
