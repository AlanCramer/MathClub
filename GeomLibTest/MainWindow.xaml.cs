using GeomLibTest.Animation;
using GeomLibTest.DrawUtil;
using GeomLibTest.GeomUtil;
using GeomLibTest.Models;
using HelixToolkit.Wpf;
using System.Collections.Generic;
using System.Diagnostics;
using System.Printing;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace GeomLibTest
{
    public partial class MainWindow : Window
    {
        private readonly List<Point3D> _startPts = new();
        private readonly List<Point3D> _destPts = new();

        private Point3DCollection _renderPoints = null!;
        private readonly PointsVisual3D _pointsVisual = new PointsVisual3D();

        private readonly RenderLoopAnimator _anim = new();

        private readonly Point3D _circleCenter = new(0, 0, 0);
        private const double CircleRadius = 2.0;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                InitializeScene();
                InitializePoints();
                InitializeRenderPoints();
                UpdatePointsVisual(0.0);
            };
        }

        private void InitializeScene()
        {
            View.Children.Clear();

            if (View.Camera is ProjectionCamera cam)
            {
                cam.Position = new Point3D(0, 0, 10);
                cam.LookDirection = new Vector3D(0, 0, -10);
                cam.UpDirection = new Vector3D(0, 1, 0);
            }

            _pointsVisual.Color = Colors.Red;
            _pointsVisual.Size = 5;
            View.Children.Add(_pointsVisual);

            var circleVisual = Primitives.DrawCircle(_circleCenter, CircleRadius, segments: 128);
            circleVisual.Color = Colors.Blue;
            circleVisual.Thickness = 1.5;
            View.Children.Add(circleVisual);
        }

        private void InitializePoints()
        {
            _startPts.Clear();
            _destPts.Clear();

            // Option A: Annulus (existing)
            //_startPts.AddRange(GeomUtil.SamplePointsInAnnulus(0.3, 3.5, 8, 24));

            // Option B: Polygon boundary sampling (example)
            var poly = new Polygon3D(new[]
            {
                 new Point3D(-3, -2, 0),
                 new Point3D( 3, -2, 0),
                 new Point3D( 4,  2, 0),
                 new Point3D( 0,  4, 0),
                 new Point3D(-4,  2, 0),
             });
            _startPts.AddRange(poly.Sample(nPerEdge: 8, closed: true));

            foreach (var p in _startPts)
                _destPts.Add(GeomUtil.Transforms.InvertPoint(p, _circleCenter, CircleRadius));

            Debug.Assert(_startPts.Count == _destPts.Count);
        }

        private void InitializeRenderPoints()
        {
            _renderPoints = new Point3DCollection(_startPts.Count);
            for (int i = 0; i < _startPts.Count; i++)
                _renderPoints.Add(new Point3D());

            _pointsVisual.Points = _renderPoints;
        }

        private void View_OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_anim.IsRunning)
            {
                Debug.WriteLine("[INFO] Animation already running; click ignored.");
                return;
            }

            _anim.Start(
                durationSeconds: 3.0,
                onTick: UpdatePointsVisual,
                onFinished: () => Debug.WriteLine("[INFO] Animation finished."));
        }

        private void UpdatePointsVisual(double t)
        {
            if (_startPts.Count != _destPts.Count) return;
            if (_renderPoints.Count != _startPts.Count) return;

            for (int i = 0; i < _startPts.Count; i++)
            {
                var a = _startPts[i];
                var b = _destPts[i];

                _renderPoints[i] = new Point3D(
                    (1 - t) * a.X + t * b.X,
                    (1 - t) * a.Y + t * b.Y,
                    (1 - t) * a.Z + t * b.Z);
            }
        }
    }
}
