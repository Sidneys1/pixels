using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media;
using System.Threading;
using System.Windows;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace srt {
    public enum RaytracerState {
        Unrendered,
        Rendering,
        Rendered,
        Aborted,
    }
    public class Raytracer {
        private int _threadCount = Environment.ProcessorCount - 1;

        private List<Thread> _threads { get; } = new();
        private ConcurrentBag<Int32Rect> _jobs { get; } = new();

        public int Samples { get; set; } = 16;
        public int Chunks { get; set; } = 10;
        public int Bounces { get; set; } = 1;
        public bool ReflectsFog { get; set; } = true;
        public long RayCount { get; private set; } = 0;

        public Scene Scene { get; set; } = new();
        public Backbuffer Backbuffer { get; set; }

        public Random Random { get; set; } = new();
        public RaytracerState State { get; private set; }


        // CAMERA
        public Point3D CameraOrigin = new(0, 0, -800); //-750
        public double CameraFocalLength { get; set; } = 200;
        double CameraFov { get; set; } = 90;

        public Raytracer(int width, int height) => Backbuffer = new(width, height);

        private Color? SampleRay(Ray r, int depth, Shape? ignore = null) {
            RayCount++;

            var intersection = this.Scene.Shapes
                .Where(s => s != ignore)
                .Select(s => Tuple.Create(s, s.Intersection(r)))
                .Where(t => t.Item2 != null && t.Item2 > 0).Select(t => Tuple.Create(t.Item1, t.Item2 ?? 0))
                .OrderBy(t => t.Item2)
                .FirstOrDefault();
            if (intersection == null) return this.Scene.FogIntensity > 0 ? this.Scene.Fog : this.Scene.Ambient;
            var originObject = intersection.Item1;
            var col1 = intersection.Item1.Sample(r);
            var intersectionRay = r * intersection.Item2;
            var IntersectionPoint = intersectionRay.End;
            var normal = intersection.Item1.Normal(IntersectionPoint);// new Ray(IntersectionPoint, IntersectionPoint - intersection.Item1.Origin).Normalize();

            Color final_color;

            if (--depth == 0 || originObject.Reflectivity == 0) {
                var amountOfFog = Math.Min(intersection.Item2 * this.Scene.FogIntensity, 1);
                col1 = col1.Lerp(this.Scene.Fog, amountOfFog);
                final_color = col1;
            } else {
                var reflection = normal;
                reflection.Direction = normal.Direction * (2.0 * (-r.Direction * normal.Direction)) + r.Direction;

                var col2 = SampleRay(reflection, depth, intersection.Item1);
                if (col2 == null) {
                    if (this.ReflectsFog) {
                        var amountOfFog = Math.Min(intersection.Item2 * this.Scene.FogIntensity, 1);
                        col1 = col1.Lerp(this.Scene.Fog, originObject.Reflectivity);
                    }
                    final_color = col1;
                } else {
                    var amountOfFog = Math.Min(intersection.Item2 * this.Scene.FogIntensity, 1);
                    // var col3 = col2.Value.Lerp(this.Scene.Fog, amountOfFog);
                    var col3 = col2.Value;
                    col3 = col1.Lerp(col3, originObject.Reflectivity);
                    amountOfFog = Math.Min(intersection.Item2 * this.Scene.FogIntensity, 1);
                    col3 = col3.Lerp(this.Scene.Fog, amountOfFog);
                    final_color = col3;
                }
            }

            // Create a ray from here to the light
            var lightPoint = new Point3D(+050, -1000, -1000);
            Ray lightRay = new(IntersectionPoint, lightPoint);
            lightRay.Origin += (normal.Direction * 0.0001);
            lightRay.Direction = lightPoint - lightRay.Origin;
            var lightDistance = lightRay.Length;
            lightRay = lightRay.Normalize();
            bool lightOccluded = this.Scene.Shapes
                .Select(s => Tuple.Create(s, s.Intersection(lightRay)))
                .Where(t => t.Item2 != null && t.Item2.Value > 0 && t.Item2.Value < lightDistance)
                .Any();

            if (lightOccluded) return final_color.Lerp(Colors.Black, 1 - this.Scene.AmbientLight);

            return final_color;
        }

        public Color Sample(double x, double y) {
            var r = new Ray(CameraOrigin, new((x / Backbuffer.Width) * 100, (y / Backbuffer.Height) * 100, CameraFocalLength)).Normalize();
            return SampleRay(r, Bounces + 1) ?? this.Scene.Ambient;
        }

        public void RenderPoint(int x, int y) {
            double halfX = this.Backbuffer.Height / 2, halfY = this.Backbuffer.Height / 2;
            if (Samples == 1) {
                var color = Sample(x + 0.5 - halfX, y + 0.5 - halfY);
                this.Backbuffer.SetPixel(x, y, color);
                return;
            }
            Color[] colors = new Color[Samples];
            for (int i = 0; i < Samples; i++)
                colors[i] = Sample(x +  Random.NextDouble() - halfX, y + Random.NextDouble() - halfY);
            this.Backbuffer.SetPixel(x, y, Color.FromRgb((byte)colors.Average(c => c.R), (byte)colors.Average(c => c.G), (byte)colors.Average(c => c.B)));
        }

        public void RenderArea(Int32Rect area) {
            for (int y = 0; y < area.Height && State != RaytracerState.Aborted; y++) {
                for (int x = 0; x < area.Width && State != RaytracerState.Aborted; x++)
                    RenderPoint(x + area.X, y + area.Y);
                // System.Threading.Thread.Sleep(5);
            }
        }

        private void RenderThread() {
            Trace.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} starting...");
            // System.Threading.Thread.Sleep(10000);
            Int32Rect area;
            Stopwatch sw = new();
            do {
                if (!_jobs.TryTake(out area)) {
                    Trace.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} exiting...");
                    break;
                }

                Trace.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} got job {area.X},{area.Y} ({area.Width}x{area.Height})...");

                sw.Restart();
                RenderArea(area);
                sw.Stop();

                Trace.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} took {sw.Elapsed.TotalMilliseconds}ms to do {area.Width * area.Height * Samples} samples ({sw.Elapsed.TotalMilliseconds / (area.Width * area.Height)}ms per pixel)");

                if (_jobs.IsEmpty && State != RaytracerState.Aborted) {
                    State = RaytracerState.Rendered;
                    StopRender();
                    break;
                }
            } while (State == RaytracerState.Rendering);
            Trace.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} exiting...");
        }

        public void StartRender(bool blocking = false) {
            RayCount = 0;
            State = RaytracerState.Rendering;
            var chunkWidth = this.Backbuffer.Width / Chunks;
            var chunkHeight = this.Backbuffer.Height / Chunks;

            List<Int32Rect> sortMe = new(Chunks * Chunks);
            for (int y = 0; y < Chunks; y++) {
                for (int x = 0; x < Chunks; x++) {
                    int width = x != (Chunks - 1) ? chunkWidth : chunkWidth + (this.Backbuffer.Width % Chunks);
                    int height = y != (Chunks - 1) ? chunkHeight : chunkHeight + (this.Backbuffer.Height % Chunks);
                    System.Diagnostics.Trace.WriteLine($"Queued {x},{y} ({width}x{height})");
                    sortMe.Add(new Int32Rect(x * chunkWidth, y * chunkHeight, width, height));
                }
            }
            foreach (var job in sortMe.OrderBy(x => this.Random.NextDouble())) _jobs.Add(job);

            for (var i = 0; i < _threadCount; i++) _threads.Add(new Thread(RenderThread));
            _threads.ForEach(t => t.Start());

            if (!blocking) return;

            Stopwatch sw = new();
            sw.Start();
            _threads.ForEach(thread => thread.Join());
            sw.Stop();
            Trace.WriteLine($"Total image took approximately {sw.Elapsed.TotalMilliseconds}ms to render.");
        }

        internal void Debug(Ray ray) {
            // // Backbuffer.SetPixel((int)ray.Origin.X, (int)ray.Origin.Y, 255, 255, 255);

            // var intersection = this.Scene.Shapes
            //     .Select(s => Tuple.Create(s, s.Intersection(ray)))
            //     .Where(t => t.Item2 != null).Select(t => Tuple.Create(t.Item1, t.Item2 ?? 0))
            //     .OrderBy(t => t.Item2)
            //     .FirstOrDefault();
            // if (intersection == null) return;
            // var originObject = intersection.Item1;
            // // ray.Direction.Z *= -1;
            // // Trace.WriteLine($"Intersection at {intersection.Item2} along ray");
            // var IntersectionPoint = (ray * intersection.Item2).End;
            // var bounce = new Ray(IntersectionPoint, IntersectionPoint - intersection.Item1.Origin);
            // bounce.Normalize();
            // // Trace.WriteLine($"{ray} -> {intersection.Item1} -> {IntersectionPoint} -> {bounce}");
            // bounce *= 25;
            // // Backbuffer.SetPixel((int)ray2point.End.X, (int)ray2point.End.Y, 255, 0, 0);

            // // Trace.WriteLine(Math.Sqrt(bounce.Direction * bounce.Direction));
            // // Trace.WriteLine($"{bounce.Origin} + {bounce.Direction} = {bounce.End}");
            // Backbuffer.DrawLine((int)Math.Floor(bounce.Origin.X), (int)Math.Floor(bounce.Origin.Y), (int)Math.Ceiling(bounce.End.X), (int)Math.Ceiling(bounce.End.Y), Colors.Red);
            // // Backbuffer.SetPixel((int)ray2point.End.X, (int)ray2point.End.Y, 255, 0, 0);
            // bounce.Normalize();
            // Trace.WriteLine(bounce);
            // // bounce.Direction *= -1;
            // // Trace.WriteLine(bounce);
            // intersection = this.Scene.Shapes
            //     .Except(new Shape[] { originObject })
            //     .Select(s => Tuple.Create(s, s.Intersection(bounce)))
            //     .Where(t => t.Item2 != null && t.Item2 > 0).Select(t => Tuple.Create(t.Item1, t.Item2 ?? 0))
            //     .OrderBy(t => t.Item2)
            //     .FirstOrDefault();
            // if (intersection == null) {
            //     bounce *= 25;
            //     Backbuffer.DrawLine((int)Math.Floor(bounce.Origin.X), (int)Math.Floor(bounce.Origin.Y), (int)Math.Ceiling(bounce.End.X), (int)Math.Ceiling(bounce.End.Y), Colors.Green);
            //     return;
            // }
            // Trace.WriteLine($"Intersects at {intersection.Item2}");
            // bounce *= intersection.Item2;
            // // Trace.WriteLine($"Intersects with {intersection.Item1} at {intersection.Item2} ({bounce.End})");
            // Backbuffer.DrawLine((int)Math.Floor(bounce.Origin.X), (int)Math.Floor(bounce.Origin.Y), (int)Math.Ceiling(bounce.End.X), (int)Math.Ceiling(bounce.End.Y), Colors.Blue);
        }

        public void StopRender() {
            if (State != RaytracerState.Rendered) State = RaytracerState.Aborted;
            // _threads.ForEach(thread => thread.Join());
            _threads.Clear();
        }
    }
}