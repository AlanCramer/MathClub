using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;

////using MathNet.Spatial.Euclidean;
//using Vector3D = MathNet.Spatial.Euclidean.Vector3D;
//using Point3D = MathNet.Spatial.Euclidean.Point3D;
//using Point2D = MathNet.Spatial.Euclidean.Point2D;

namespace GeomLibTest
{
    public partial class MainWindow : Window
    {
        private readonly List<Point3D> _startPts = new();
        private readonly List<Point3D> _destPts = new();

        private Point3DCollection _renderPoints;

        private readonly PointsVisual3D _pointsVisual = new PointsVisual3D();
        private DispatcherTimer _timer;
        private double _t; // 0 → 1 interpolation parameter
        private bool _isAnimating;

        // Inversion circle
        private readonly Point3D _circleCenter = new Point3D(0, 0, 0);
        private const double CircleRadius = 2.0;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                InitializeScene();
                InitializePoints();
                InitializeRenderPoints();

                // IMPORTANT: No animation on load.
                // We just show the sampled data.
            };
        }

        private void InitializeScene()
        {
            View.Children.Clear();

            if (View.Camera is ProjectionCamera cam)
            {
                cam.Position = new System.Windows.Media.Media3D.Point3D(0, 0, 10);
                cam.LookDirection = new System.Windows.Media.Media3D.Vector3D(0, 0, -10);
                cam.UpDirection = new System.Windows.Media.Media3D.Vector3D(0, 1, 0);
            }

            // Configure point visual
            _pointsVisual.Color = Colors.Red;
            _pointsVisual.Size = 5;
            View.Children.Add(_pointsVisual);

            // Draw the inversion circle
            var circleVisual = DrawCircle(_circleCenter, CircleRadius, segments: 128);
            circleVisual.Color = Colors.Blue;
            circleVisual.Thickness = 1.5;
            View.Children.Add(circleVisual);
        }

        private void InitializePoints()
        {
            _startPts.Clear();
            _destPts.Clear();


            //_startPts.AddRange(SamplePointsInRect(
            //    new Point2D(-10, -10), 
            //    new Point2D(10, 10),
            //    20, 20));

            _startPts.AddRange(SamplePointsInAnnulus(
                innerRadius: 0.3,
                outerRadius: 3.5,
                radialCount: 8,
                angularCount: 24));

            foreach (var p in _startPts)
            {
                _destPts.Add(InvertPoint(p, _circleCenter, CircleRadius));
            }

            Debug.Assert(_startPts.Count == _destPts.Count,
                "Start and destination lists should have the same length.");

            // Show just the *start* data initially
            UpdatePointsVisual(0.0);
        }


        private void InitializeRenderPoints()
        {
            _renderPoints = new Point3DCollection(_startPts.Count);
            for (int i = 0; i < _startPts.Count; i++)
            {
                // initial dummy values; will be overwritten in UpdatePointsVisual
                _renderPoints.Add(new Point3D());
            }

            _pointsVisual.Points = _renderPoints;
        }

        /// <summary>
        /// Mouse click in the viewport → start the animation if not already running.
        /// </summary>
        private void View_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_isAnimating)
            {
                Debug.WriteLine("[INFO] Animation already in progress; click ignored.");
                return;
            }

            // Optional: Reset camera or log click position if you want.
            // var pos = e.GetPosition(View);
            // Debug.WriteLine($"Click at {pos}");

            StartAnimation();
        }

        private void StartAnimation()
        {
            if (_startPts.Count == 0 || _destPts.Count == 0)
            {
                Debug.WriteLine("[WARN] No points to animate.");
                return;
            }

            if (_startPts.Count != _destPts.Count)
            {
                Debug.WriteLine("[ERROR] Mismatched start/destination point counts.");
                return;
            }

            _t = 0.0;
            _isAnimating = true;

            _timer?.Stop();
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60Hz
            };

            _timer.Tick += (_, _) =>
            {
                const double speed = 0.4; // fraction per second
                _t += speed * _timer.Interval.TotalSeconds;

                if (_t >= 1.0)
                {
                    _t = 1.0;
                    _timer.Stop();
                    _isAnimating = false;
                    Debug.WriteLine("[INFO] Animation finished.");
                }

                UpdatePointsVisual(_t);
            };

            Debug.WriteLine("[INFO] Animation started.");
            _timer.Start();
        }

        /// <summary>
        /// Interpolates between startPts and destPts with parameter t in [0,1].
        /// </summary>
        private void UpdatePointsVisual(double t)
        {
            if (_startPts.Count != _destPts.Count)
            {
                Debug.WriteLine("[WARN] Start/destination counts differ; not updating.");
                return;
            }

            if (_renderPoints == null || _renderPoints.Count != _startPts.Count)
            {
                Debug.WriteLine("[WARN] RenderPoints not initialized or wrong size.");
                return;
            }

            for (int i = 0; i < _startPts.Count; i++)
            {
                var a = _startPts[i]; // Math3D.Point3D
                var b = _destPts[i];  // Math3D.Point3D

                double x = (1 - t) * a.X + t * b.X;
                double y = (1 - t) * a.Y + t * b.Y;
                double z = (1 - t) * a.Z + t * b.Z;

                _renderPoints[i] = new Point3D(x, y, z);
            }

            // No need to reassign _pointsVisual.Points — it’s the same collection.
        }


        #region Geometry helpers

        private static Point3D InvertPoint(Point3D p, Point3D center, double radius)
        {
            var v = p - center;
            var r2 = radius * radius;
            var d2 = v.X * v.X + v.Y * v.Y + v.Z * v.Z;

            const double Eps = 1e-9;
            if (d2 < Eps)
            {
                Debug.WriteLine("[WARN] Inversion of center is undefined; returning far point.");
                return new Point3D(center.X + 1000, center.Y, center.Z);
            }

            var scale = r2 / d2;
            var inv = center + scale * v;
            return inv;
        }


        //private static IEnumerable<Point3D> SamplePointsInRect(
        //    Point2D lowerleft,
        //    Point2D upperright,
        //    int countX,
        //    int countY
        //)
        //{
        //    if (countX <= 0 || countY <= 0)
        //        throw new ArgumentException("x and y count must be positive.");

        //    var pts = new List<Point3D>();

        //    for (int i = 0; i < countX; i++)
        //    {
        //        double t = (i + 0.5) / countX;

        //        for (int j = 0; j < countY; j++)
        //        {
        //            pts.Add(new Point3D(i, j, 0));
        //        }
        //    }

        //    return pts;
        //}

        private static IEnumerable<Point3D> SamplePointsInAnnulus(
            double innerRadius,
            double outerRadius,
            int radialCount,
            int angularCount)
        {
            if (radialCount <= 0 || angularCount <= 0)
                throw new ArgumentException("radialCount and angularCount must be positive.");

            var pts = new List<Point3D>();

            for (int i = 0; i < radialCount; i++)
            {
                double t = (i + 0.5) / radialCount;
                double r = innerRadius + t * (outerRadius - innerRadius);

                for (int j = 0; j < angularCount; j++)
                {
                    double angle = 2.0 * Math.PI * j / angularCount;
                    double x = r * Math.Cos(angle);
                    double y = r * Math.Sin(angle);
                    pts.Add(new Point3D(x, y, 0));
                }
            }

            return pts;
        }

        private static LinesVisual3D DrawCircle(
            Point3D center,
            double radius,
            int segments = 64)
        {
            if (segments < 3)
                throw new ArgumentException("segments must be at least 3.", nameof(segments));

            var lines = new LinesVisual3D();

            var pts = new List<System.Windows.Media.Media3D.Point3D>();
            for (int i = 0; i <= segments; i++)
            {
                double t = (double)i / segments;
                double angle = 2.0 * Math.PI * t;

                double x = center.X + radius * Math.Cos(angle);
                double y = center.Y + radius * Math.Sin(angle);

                pts.Add(new System.Windows.Media.Media3D.Point3D(x, y, center.Z));
            }

            for (int i = 0; i < pts.Count - 1; i++)
            {
                lines.Points.Add(pts[i]);
                lines.Points.Add(pts[i + 1]);
            }

            return lines;
        }

        #endregion
    }
}
