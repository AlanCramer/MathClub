using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;


namespace AngryParabolas
{
    public partial class MainWindow : Window
    {
        private LinesVisual3D? _axes;
        private SphereVisual3D? _ballVis;  // visual representation of the _ball
        private ArrowVisual3D? _aimArrow;

        private readonly Stopwatch _stopwatch = new();
        private readonly DispatcherTimer _uiTimer;

        private bool _isLaunched;
        private bool _isAiming;

        private Ball? _ball;
        private double _prevT;

        private Point2D _launchPosition;
        private Vector2D _launchVelocity;
        private const double Gravity = -9.81;

        public MainWindow()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            _uiTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS for UI text
            };
            _uiTimer.Tick += UiTimerOnTick;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("[INFO] MainWindow loaded, initializing scene…");

            SetupCamera();
            CreateScene();

            View.MouseLeftButtonDown += OnViewportMouseLeftButtonDown;
            View.MouseMove += OnViewportMouseMove;
            CompositionTarget.Rendering += OnRendering;

            ResetSimulation();
        }

        private void SetupCamera()
        {
            // Orthographic top-down-ish view of the XY plane
            var cam = new OrthographicCamera
            {
                Position = new Point3D(10, 6, 30),
                LookDirection = new Vector3D(0, 0, -30),
                UpDirection = new Vector3D(0, 1, 0),
                Width = 30
            };

            View.Camera = cam;
        }

        private void CreateScene()
        {
            View.Children.Clear();

            // ✅ Add lights so the ball is visible
            View.Children.Add(new DefaultLights());

            // XY axes as simple lines (use bright colors)
            _axes = new LinesVisual3D
            {
                Thickness = 2.0,
                Color = Colors.White
            };

            // X axis
            _axes.Points.Add(new Point3D(-15, 0, 0));
            _axes.Points.Add(new Point3D(15, 0, 0));

            // Y axis
            _axes.Points.Add(new Point3D(0, -10, 0));
            _axes.Points.Add(new Point3D(0, 10, 0));

            View.Children.Add(_axes);

            // Ball at origin – make it bright
            _ballVis = new SphereVisual3D
            {
                Center = new Point3D(0, 0, 0),
                Radius = 0.5,
                PhiDiv = 32,
                ThetaDiv = 64,
                Material = MaterialHelper.CreateMaterial(Brushes.Yellow)
            };
            View.Children.Add(_ballVis);

            // Aim vector arrow – bright and a bit thicker
            _aimArrow = new ArrowVisual3D
            {
                Point1 = new Point3D(0, 0, 0),
                Point2 = new Point3D(0, 0, 0),
                Diameter = 0.25,
                Fill = Brushes.Cyan,
                Visible = false
            };
            View.Children.Add(_aimArrow);
        }
        private void ResetSimulation()
        {
            Debug.WriteLine("[INFO] Resetting simulation state.");

            _isLaunched = false;
            _isAiming = false;

            _ball = null;

            _launchVelocity = new Vector2D(0, 0);
            _launchPosition = new Point2D(0, 0);

            _ballVis.Center = _launchPosition.ToP3();
            _aimArrow.Visible = false;

            _stopwatch.Reset();
            _uiTimer.Stop();

            TimerText.Text = "t = 0.00 s";
            StatusText.Text = "Click to choose launch direction";
        }


        private void OnViewportMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var mouse = e.GetPosition(View);
            Debug.WriteLine($"[DEBUG] Mouse click at: {mouse}");

            // If we're already in flight, a click resets the simulation.
            if (_isLaunched)
            {
                Debug.WriteLine("[INFO] Click during flight -> reset simulation.");
                ResetSimulation();
                return;
            }

            var hit = ProjectToZPlane(mouse);
            if (!hit.HasValue)
            {
                Debug.WriteLine("[WARN] Could not project mouse to z=0 plane on click.");
                return;
            }

            var hitPoint = hit.Value;
            Debug.WriteLine($"[DEBUG] Click projected to z=0: {hitPoint}");

            if (!_isAiming)
            {
                // 👉 First click: start rubber-band aiming
                var dir = hitPoint - new Point3D(0, 0, 0);
                if (dir.Length < 0.1)
                {
                    Debug.WriteLine("[INFO] Aim vector too short on first click, ignoring.");
                    return;
                }

                _aimArrow.Point1 = new Point3D(0, 0, 0);
                _aimArrow.Point2 = hitPoint;
                _aimArrow.Visible = true;

                _isAiming = true;

                StatusText.Text = "Move mouse to adjust vector, click again to launch";
                Debug.WriteLine($"[INFO] Aiming started. Initial aim head: {hitPoint}");
            }
            else
            {
                var aimVec3 = _aimArrow.Point2 - _aimArrow.Point1;
                var length = aimVec3.Length;

                if (length < 0.1)
                {
                    Debug.WriteLine("[WARN] Final aim vector too short, ignoring launch.");
                    StatusText.Text = "Vector too short. Move mouse further and click again.";
                    return;
                }

                // 2D direction from the 3D arrow (drop Z)
                var dir2 = aimVec3.ToV2().Normalized();
                var speed = length * 3.0;

                _launchPosition = new Point2D(0, 0);
                _launchVelocity = dir2 * speed;

                _isAiming = false;

                Launch(_launchPosition, dir2, speed);

            }
        }

        private void OnViewportMouseMove(object sender, MouseEventArgs e)
        {
            if (!_isAiming || _isLaunched)
                return;

            var mouse = e.GetPosition(View);
            var hit = ProjectToZPlane(mouse);
            if (!hit.HasValue)
                return;

            var hitPoint = hit.Value;

            _aimArrow.Point2 = hitPoint;

            var aimVec = _aimArrow.Point2 - _aimArrow.Point1;
            var length = aimVec.Length;

            StatusText.Text =
                $"Aiming: vector = ({aimVec.X:0.0}, {aimVec.Y:0.0}), |v| ≈ {length * 3.0:0.0}";

        }



        /// <summary>
        /// Project a 2D screen point onto the world plane z = 0.
        /// Uses Helix's Viewport3DHelper to get near/far points and
        /// then does manual line-plane intersection to avoid Plane3D.RayIntersection.
        /// </summary>
        private Point3D? ProjectToZPlane(Point screenPos)
        {
            if (View.Viewport == null)
            {
                Debug.WriteLine("[WARN] View.Viewport is null.");
                return null;
            }

            Point3D near, far;
            if (!Viewport3DHelper.Point2DtoPoint3D(View.Viewport, screenPos, out near, out far))
            {
                Debug.WriteLine("[WARN] Point2DtoPoint3D failed.");
                return null;
            }

            var rayDir = far - near;

            // If the ray is parallel to the z=0 plane (rayDir.Z ≈ 0), no intersection.
            if (Math.Abs(rayDir.Z) < 1e-6)
            {
                Debug.WriteLine("[WARN] Ray is parallel to z=0 plane.");
                return null;
            }

            // Solve near.Z + t * rayDir.Z = 0  ->  t = -near.Z / rayDir.Z
            var t = -near.Z / rayDir.Z;
            if (t < 0)
            {
                Debug.WriteLine("[WARN] Intersection behind the camera.");
                return null;
            }

            var hit = near + t * rayDir;
            return hit;
        }

        private void Launch(Point2D startPos, Vector2D dirUnit, double speed)
        {
            Debug.Assert(Math.Abs(dirUnit.Length - 1.0) < 1e-6, "dirUnit should be normalized.");

            _ball = new Ball(startPos, dirUnit * speed, radius: 0.5)
            {
                Acceleration = new Vector2D(0, Gravity), // or (0,0)
                Restitution = 0.95
            };

            _isLaunched = true;

            _stopwatch.Restart();        // important
            _prevT = 0.0;                // since stopwatch restarted
            _uiTimer.Start();

            StatusText.Text = "In flight (click to reset)";
        }


        private void UiTimerOnTick(object? sender, EventArgs e)
        {
            if (!_isLaunched)
                return;

            var t = _stopwatch.Elapsed.TotalSeconds;
            TimerText.Text = $"t = {t:0.00} s";
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (!_isLaunched || _ball is null) return;

            var t = _stopwatch.Elapsed.TotalSeconds;
            var dt = t - _prevT;
            _prevT = t;

            // Common failure mode after breakpoints / window drag:
            if (dt <= 0) return;
            if (dt > 0.05) dt = 0.05;

            _ball.Step(dt);

            // Render
            _ballVis.Center = _ball.Position.ToP3();
        }

    }
}
