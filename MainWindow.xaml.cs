#define TRACE

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using srt;

namespace pixels {

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        private Int32Rect Area = new(0, 0, WIDTH, HEIGHT);
        private readonly System.Timers.Timer FpsTimer = new(250);

        public WriteableBitmap Image;

        const int SCALE = 1;
        const int WIDTH = 750;
        const int HEIGHT = 750;

        volatile int frames = 0;

        readonly Raytracer Rt = new(WIDTH, HEIGHT);

        public static RoutedCommand SaveCommand = new(), IncreaseFocalLengthCommand = new(), DecreaseFocalLengthCommand = new(), ForwardCommand = new(), BackCommand = new();

        public MainWindow() {
            Image = new(WIDTH, HEIGHT, 96, 96, PixelFormats.Rgb24, null);

            Rt.Scene.Ambient = Colors.Gray;
            Rt.Scene.Fog = Colors.Gray;
            Rt.Scene.FogIntensity = 0.0001;
            // Rt.Samples = 1;
            // Rt.Samples = 4;
            // Rt.Bounces = 2;

            Rt.Scene.Shapes.Add(new Sphere(new Point3D(+000, +000, +200), 100, Colors.Silver) { Reflectivity = 0.1 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(-150, +000, +400), 100, Colors.Yellow) { Reflectivity = 0.75 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(+150, +000, +400), 100, Colors.Purple) { Reflectivity = 0.25 });

            Rt.Scene.Shapes.Add(new Sphere(new Point3D(-150, -150, +050), 50, Colors.Red) { Reflectivity = 0.9 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(+000, -150, +100), 50, Colors.Green) { Reflectivity = 0.75 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(+150, -150, +250), 50, Colors.Blue) { Reflectivity = 0.5 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(-150, +150, +400), 50, Colors.Blue) { Reflectivity = 0.25 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(+000, +150, +550), 50, Colors.Green) { Reflectivity = 0.1 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(+150, +150, +700), 50, Colors.Red) { Reflectivity = 0 });

            Rt.Scene.Shapes.Add(new Sphere(new Point3D(+150, +150, +050), 50, Colors.Red) { Reflectivity = 0 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(-150, +150, +050), 50, Colors.Black) { Reflectivity = 0.1, Refract = true, RefractiveCoefficient = 1.5 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(-000, +150, +050), 50, Colors.Black) { Reflectivity = 0.1, Refract = true, RefractiveCoefficient = 1.01 });

            Rt.Scene.Shapes.Add(new Plane(new Point3D(0, +200, 0), new Point3D(0, -1, 0), Colors.LightGray));

            SaveCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            IncreaseFocalLengthCommand.InputGestures.Add(new KeyGesture(Key.I, ModifierKeys.Control));
            DecreaseFocalLengthCommand.InputGestures.Add(new KeyGesture(Key.K, ModifierKeys.Control));
            ForwardCommand.InputGestures.Add(new KeyGesture(Key.Up));
            BackCommand.InputGestures.Add(new KeyGesture(Key.Down));
            Closed += On_Closed;
            InitializeComponent();

            imageCtl.Width = WIDTH * SCALE;
            imageCtl.Height = HEIGHT * SCALE;
            imageCtl.Source = Image;
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(imageCtl, BitmapScalingMode.NearestNeighbor);

            var t = new System.Timers.Timer(25);
            t.AutoReset = true;
            t.Elapsed += TimerElapsed;
            t.Start();

            FpsTimer.AutoReset = true;
            FpsTimer.Elapsed += FpsTimerElapsed;
            FpsTimer.Start();

            Rt.StartRender();
        }

        private void SaveCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
            if (Rt.State != RaytracerState.Rendered) return;
            FpsTimer.Stop();
            using (FileStream stream = new(".\\image.png", FileMode.Create)) {
                PngBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(Image));
                encoder.Save(stream);
                Title = $"Saved to {stream.Name}";
            }
        }

        private void IncreaseFocalLengthCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
            if (Rt.State != RaytracerState.Rendered) return;
            Rt.CameraFocalLength += 50;
            Rt.StartRender();
            FpsTimer.Start();
        }

        private void DecreaseFocalLengthCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
            if (Rt.State != RaytracerState.Rendered) return;
            Rt.CameraFocalLength -= 50;
            Rt.StartRender();
            FpsTimer.Start();
        }

        private void ForwardCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
            if (Rt.State != RaytracerState.Rendered) return;

            Rt.CameraOrigin.Z += 50;
            Rt.StartRender();
            FpsTimer.Start();
        }

        private void BackCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
            if (Rt.State != RaytracerState.Rendered) return;

            Rt.CameraOrigin.Z -= 50;
            Rt.StartRender();
            FpsTimer.Start();
        }

        void On_Closed(Object? o, EventArgs e) => Rt.StopRender();

        double GetDistance(double x1, double y1, double x2, double y2) {
            var x = (x1 - x2);
            var y = (y1 - y2);
            return Math.Sqrt((x * x) + (y * y));
        }

        private void TimerElapsed(object? sender, System.Timers.ElapsedEventArgs e) {
            if (Rt.Backbuffer.Dirty)
                Image.Dispatcher.Invoke(DispatchElapsed);
        }

        private void FpsTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e) {
            string title = $"SRT - Camera(O:{Rt.CameraOrigin}, FL:{Rt.CameraFocalLength}) - {Rt.RayCount:N0} Rays";
            if (Rt.State == RaytracerState.Rendered) {
                FpsTimer.Stop();
                title += $" - Done";
            } else
                title += $" - {Rt.State} - {frames * 4} fps";
            this.Dispatcher.BeginInvoke(() => {
                this.Title = title;
                frames = 0;
            });
        }

        private void DispatchElapsed() {
            Rt.Backbuffer.CopyToBitmap(Image, Area);
            frames++;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseEventArgs e) {
            if (Rt.State != RaytracerState.Rendered) return;
            FpsTimer.Stop();
            var pos = e.GetPosition(imageCtl);
            pos.X /= SCALE;
            pos.Y /= SCALE;
            // Shape? closest = null;
            // double closestIntersection = double.MaxValue;
            Ray ray = new(pos.X - Rt.Backbuffer.Width / 2.0, pos.Y - Rt.Backbuffer.Height / 2.0, 0, 0, 0, 1);
            // foreach (var shape in Rt.Scene.Shapes) {
            //     var intersection = shape.Intersection(ray);
            //     if (intersection == null || intersection > closestIntersection) continue;
            //     closest = shape;
            //     closestIntersection = intersection.Value;
            // }
            // if (closest == null) {
            //     Title = "No Hit";
            //     return;
            // }
            Title = $"{ray.Origin}";

            Rt.Debug(ray);
        }
    }
}
