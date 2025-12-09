using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf;

namespace HelloHelix
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Loaded += (_, _) =>
            {
                InitializeScene();
            };
        }

        private void InitializeScene()
        {
            View.Children.Clear();

            var cam = View.Camera as ProjectionCamera;
            if (cam != null)
            {
                cam.Position = new Point3D(0, 0, 10);
                cam.LookDirection = new Vector3D(0, 0, -10);
                cam.UpDirection = new Vector3D(0, 1, 0);
            }

            var xAxis = CreateLine(
                new Point3D(0, 0, 0),
                new Point3D(5, 0, 0),
                Colors.Red,
                thickness: 2);

            var yAxis = CreateLine(
                new Point3D(0, 0, 0),
                new Point3D(0, 5, 0),
                Colors.Green,
                thickness: 2);

            View.Children.Add(xAxis);
            View.Children.Add(yAxis);

            var trianglePoints = new[]
            {
                new Point3D(0,   0, 0),
                new Point3D(3,   1, 0),
                new Point3D(1.5, 2, 0),
                new Point3D(0,   0, 0)
            };

            var triangle = CreatePolyline(trianglePoints, Colors.Blue, thickness: 2);
            View.Children.Add(triangle);

            var label = new BillboardTextVisual3D
            {
                Position = new Point3D(0.5, 2.5, 0),
                Text = "Hello, world!",
                FontSize = 24,
                Foreground = System.Windows.Media.Brushes.Black
            };
            View.Children.Add(label);
        }

        private static LinesVisual3D CreateLine(
            Point3D start,
            Point3D end,
            Color color,
            double thickness = 1.0)
        {
            return new LinesVisual3D
            {
                Points = new Point3DCollection { start, end },
                Color = color,
                Thickness = thickness
            };
        }

        private static LinesVisual3D CreatePolyline(
            IEnumerable<Point3D> points,
            Color color,
            double thickness = 1.0)
        {
            var vertices = points.ToList();
            var collection = new Point3DCollection();

            // Each pair of consecutive vertices becomes a segment
            for (int i = 0; i < vertices.Count - 1; i++)
            {
                collection.Add(vertices[i]);
                collection.Add(vertices[i + 1]);
            }

            return new LinesVisual3D
            {
                Points = collection,
                Color = color,
                Thickness = thickness
            };
        }
    }
}
