#define TRACE

using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using srt;

namespace pixels
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Int32Rect Area = new(0, 0, WIDTH, HEIGHT);
        private readonly System.Timers.Timer FpsTimer = new(250);

        public WriteableBitmap Image;

        const int SCALE = 1;
        const int WIDTH = 1000;
        const int HEIGHT = 1000;

        volatile int frames = 0;

        readonly Raytracer Rt = new(WIDTH, HEIGHT);

        public static RoutedCommand MyCommand = new();

        public MainWindow()
        {
            Image = new(WIDTH, HEIGHT, 96, 96, PixelFormats.Rgb24, null);

            Rt.Scene.Ambient = Colors.Gray;
            Rt.Scene.Fog = Colors.Gray;
            Rt.Scene.FogIntensity = 0.0005;
            Rt.Samples = 1;
            Rt.Bounces = 10;
            // Rt.Scene.Shapes.Add(new Sphere(new Point3D(100, 100, 2000), 250, Colors.LimeGreen) { Reflectivity = 1.0 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(250 - 250, 250 - 250, 200), 100, Colors.Silver) { Reflectivity = 0.9 });
            // Rt.Scene.Shapes.Add(new Sphere(new Point3D(0, 0, 100), 25, Colors.Magenta) { Reflectivity = 0.9 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(100 - 250, 250 - 250, 400), 100, Colors.Yellow) { Reflectivity = 0.75 });
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(400 - 250, 250 - 250, 400), 100, Colors.Purple) { Reflectivity = 0.75 });

            Rt.Scene.Shapes.Add(new Sphere(new Point3D(100 - 250, 100 - 250, 50), 50, Colors.Red) { Reflectivity = 0.5});
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(250 - 250, 100 - 250, 100), 50, Colors.Green) { Reflectivity = 0.5});
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(400 - 250, 100 - 250, 250), 50, Colors.Blue) { Reflectivity = 0.5});
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(100 - 250, 400 - 250, 400), 50, Colors.Blue) { Reflectivity = 0.25});
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(250 - 250, 400 - 250, 550), 50, Colors.Green) { Reflectivity = 0.25});
            Rt.Scene.Shapes.Add(new Sphere(new Point3D(400 - 250, 400 - 250, 700), 50, Colors.Red) { Reflectivity = 0.5});

            MyCommand.InputGestures.Add(new KeyGesture(Key.S, ModifierKeys.Control));
            Closed += On_Closed;
            InitializeComponent();

            imageCtl.Width = WIDTH * SCALE;
            imageCtl.Height = HEIGHT * SCALE;
            imageCtl.Source = Image;
            System.Windows.Media.RenderOptions.SetBitmapScalingMode(imageCtl, BitmapScalingMode.NearestNeighbor);

            var t = new System.Timers.Timer(10);
            t.AutoReset = true;
            t.Elapsed += TimerElapsed;
            t.Start();

            FpsTimer.AutoReset = true;
            FpsTimer.Elapsed += FpsTimerElapsed;
            FpsTimer.Start();

            Rt.StartRender();
        }

        private void MyCommandExecuted(object sender, ExecutedRoutedEventArgs e) {
            if (Rt.State != RaytracerState.Rendered) return;
            FpsTimer.Stop();
            using (FileStream stream = new(".\\image.png", FileMode.Create)) {
                PngBitmapEncoder encoder = new();
                encoder.Frames.Add(BitmapFrame.Create(Image));
                encoder.Save(stream);
                Title = $"Saved to {stream.Name}";
            }
        }

        void On_Closed(Object? o, EventArgs e) => Rt.StopRender();

        double GetDistance(double x1, double y1, double x2, double y2) 
        {
            var x = (x1 - x2);
            var y = (y1 - y2);
            return Math.Sqrt((x * x) + (y * y));
        }
    
        private void TimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (Rt.Backbuffer.Dirty)
                Image.Dispatcher.Invoke(DispatchElapsed);
        }

        private void FpsTimerElapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            if (Rt.State == RaytracerState.Rendered) {
                FpsTimer.Stop();
                return;
            }
            this.Dispatcher.BeginInvoke(() => {
                this.Title = $"SRT - {frames * 4} fps - {Rt.State}";
                frames = 0;
            });
        }

        private void DispatchElapsed() {
            Rt.Backbuffer.CopyToBitmap(Image, Area);
            frames++;
        }

        private void Image_MouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            if (Rt.State != RaytracerState.Rendered) return;
            FpsTimer.Stop();
            var pos = e.GetPosition(imageCtl);
            pos.X /= SCALE;
            pos.Y /= SCALE;
            Shape? closest = null;
            double closestIntersection = double.MaxValue;
            Ray ray = new(pos.X, pos.Y, 0, 0, 0, 1);
            foreach (var shape in Rt.Scene.Shapes) {
                var intersection = shape.Intersection(ray);
                if (intersection == null || intersection > closestIntersection) continue;
                closest = shape;
                closestIntersection = intersection.Value;
            }
            if (closest == null) {
                Title = "No Hit";
                return;
            }
            Title = $"{pos}: {closest} ({closestIntersection})";

            Rt.Debug(ray);
        }
    }
}
