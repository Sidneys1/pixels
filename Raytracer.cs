using System;
using System.Linq;
using System.Collections.Generic;
// using System.Windows.Media;
using System.Threading;
using System.Windows;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace srt {
    using Color3 = Point3D;
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
        public int LightSamples { get; set; } = 16;
        public int Chunks { get; set; } = 10;
        public int Bounces { get; set; } = 4;
        public bool ReflectsFog { get; set; } = true;
        public long RayCount { get; private set; } = 0;

        public Scene Scene { get; set; } = new();
        public Backbuffer Backbuffer { get; set; }

        public Random Random { get; set; } = new();
        public RaytracerState State { get; private set; }


        // CAMERA
        public Point3D CameraOrigin = new(0, 0, -800);
        public double CameraFocalLength { get; set; } = 200;

        public Raytracer(int width, int height) => Backbuffer = new(width, height);

        public static double FresnelReflectAmount(double n1, double n2, Point3D normal, Point3D incident, double reflectivity) {
            double r0 = (n1 - n2) / (n1 + n2);
            r0 *= r0;

            double cosX = (normal * incident);
            if (n1 > n2) {
                double n = n1 / n2;
                double sinT2 = n * n * (1.0 - cosX * cosX);
                // Total internal reflection
                if (sinT2 > 1) return 1.0;
                cosX = Math.Sqrt(1.0 - sinT2);
            }
            double x = 1.0 - cosX;
            double ret = r0 + (1.0 - r0) * x * x * x * x * x;

            // Adjust based on reflectance
            ret = (reflectivity + (1.0 - reflectivity) * ret);
            return ret;

            // double R_0 = Math.Pow((n1 - n2) / (n1 + n2), 2);
            // double dot = -(normal * -incident);
            // double R = R_0 + (1 - R_0) * Math.Pow(1 - dot, 5);
            // return R;
        }

        // public static Point3D Refract(Point3D incident, Point3D normal, double n1, double n2) {
        //     double n = n1 / n2;
        //     double cosI = (normal * incident);
        //     double sinT2 = n * n * (1.0 - cosI * cosI);
        //     if (sinT2 > 1.0) return new Point3D();
        //     double cosT = Math.Sqrt(1.0 - sinT2);
        //     return incident * n + normal * (n * cosI - cosT);
        // }

        public static Point3D? Refract(Point3D I, Point3D N, double ior) {
            double cosi = Math.Clamp((I * N), -1, 1);
            double etai = 1, etat = ior;
            Point3D n = N;
            if (cosi < 0) {
                cosi = -cosi;
            } else {
                var x = etai;
                etai = etat;
                etat = x;
                n = -N;
            }
            double eta = etai / etat;
            double k = 1 - eta * eta * (1 - cosi * cosi);
            return k < 0 ? null : (I * eta) + (n * (eta * cosi - Math.Sqrt(k)));
        }

        private Color3? SampleRay(Ray r, int depth, Shape? ignore = null) {
            RayCount++;
            depth--;

            var intersection = this.Scene.Shapes
                .Where(s => s != ignore)
                .Select(s => Tuple.Create(s, s.Intersection(r)))
                .Where(t => t.Item2 != null && t.Item2 > 0).Select(t => Tuple.Create(t.Item1, t.Item2 ?? 0))
                .OrderBy(t => t.Item2)
                .FirstOrDefault();
            if (intersection == null) return this.Scene.FogIntensity > 0 ? this.Scene.Fog : this.Scene.Ambient;
            var originObject = intersection.Item1;
            var intersectionRay = r * intersection.Item2;
            var IntersectionPoint = intersectionRay.End;
            var normal = intersection.Item1.Normal(IntersectionPoint);// new Ray(IntersectionPoint, IntersectionPoint - intersection.Item1.Origin).Normalize();

            Color3 originObjectColor;
            if (originObject.Refract && depth != 0) {
                var refractedRay = originObject.RefractRay(r, IntersectionPoint, normal);
                if (refractedRay == null) originObjectColor = System.Windows.Media.Colors.Magenta;//originObject.Sample(r);
                else originObjectColor = SampleRay(refractedRay.Value, depth + 1, originObject) ?? this.Scene.Ambient;
            } else
                originObjectColor = originObject.Sample(r);

            Color3 final_color;

            if (depth != 0 && originObject.Reflectivity != 0) {
                var reflection = normal;
                reflection.Direction = (normal.Direction * (2.0 * (-r.Direction * normal.Direction)) + r.Direction).Normalize();

                var reflectedColor = SampleRay(reflection, depth, originObject);
                if (reflectedColor == null) {
                    if (this.ReflectsFog) {
                        originObjectColor = originObjectColor.Lerp(this.Scene.Fog, originObject.Reflectivity);
                    }
                    final_color = originObjectColor;
                } else {
                    double by = FresnelReflectAmount(1, originObject.RefractiveCoefficient, normal.Direction, reflection.Direction, originObject.Reflectivity);
                    final_color = originObjectColor.Lerp(reflectedColor.Value, by);
                }
            } else
                final_color = originObjectColor;

            /******* Lighting ********/
            var lightPoint = new Point3D(+050, -500, -500);
            var lightRadius = 100.0;
            int occluded = 0;
            Ray lightRay = new(IntersectionPoint, (lightPoint - IntersectionPoint).Normalize());
            double dot = Math.Clamp(Scene.AmbientLight + (lightRay.Direction * normal.Direction), 0, 1);
            lightRay.Origin += (normal.Direction * 0.0001);
            for (int i = 0; i < LightSamples; i++) {
                RayCount++;
                var mod = new Point3D((Random.NextDouble() - 0.5), (Random.NextDouble() - 0.5), (Random.NextDouble() - 0.5)).Normalize() * lightRadius /* * Random.NextDouble() */;
                var pointAt = lightPoint + mod;
                lightRay.Direction = (pointAt - lightRay.Origin);
                var lightDistance = lightRay.Length;
                lightRay = lightRay.Normalize();
                bool lightOccluded = this.Scene.Shapes
                    // .Where(s => !s.Refract)
                    .Select(s => s.Intersection(lightRay))
                    .Where(t => t != null && t.Value > 0 && t.Value < lightDistance)
                    .Any();
                if (lightOccluded) occluded++;
            }

            final_color = final_color.Lerp(final_color * Scene.AmbientLight, occluded / (double)LightSamples) * dot;

            // Apply fog to *this ray*
            var amountOfFog = Math.Min(intersection.Item2 * this.Scene.FogIntensity, 1);
            return final_color.Lerp(this.Scene.Fog, amountOfFog);
        }

        public Color3 Sample(double x, double y) {
            var r = new Ray(CameraOrigin, new((x / Backbuffer.Width) * 100, (y / Backbuffer.Height) * 100, CameraFocalLength)).Normalize();
            // var r = new Ray(x, y, 0, 0, 0, 1);
            return SampleRay(r, Bounces + 1) ?? this.Scene.Ambient;
        }

        public void RenderPoint(int x, int y) {
            double halfX = this.Backbuffer.Height / 2, halfY = this.Backbuffer.Height / 2;
            if (Samples == 1) {
                var color = Sample(x + 0.5 - halfX, y + 0.5 - halfY);
                this.Backbuffer.SetPixel(x, y, color);
                return;
            }
            Color3[] colors = new Color3[Samples];
            for (int i = 0; i < Samples; i++)
                colors[i] = Sample(x + Random.NextDouble() - halfX, y + Random.NextDouble() - halfY);
            var averageColor = colors.Aggregate((agg, current) => agg + current) / Samples;
            this.Backbuffer.SetPixel(x, y, averageColor);
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
            // ray.Origin.Z = Scene.Shapes[0].Origin.Z;
            // ray.Direction = new(1, 0, 0);

            // var intersection = Scene.Shapes[0].Intersection(ray);
            // if (intersection == null) {
            //     Trace.WriteLine("No hit");
            //     return;
            // }
            // var screenSpace = ray;
            // screenSpace *= intersection.Value;
            // screenSpace.Origin += new Point3D(Backbuffer.Width / 2.0, Backbuffer.Height / 2.0, 100);
            // var ssEnd = screenSpace.End;
            // Backbuffer.DrawLine((int)screenSpace.Origin.X, (int)screenSpace.Origin.Y, (int)ssEnd.X, (int)ssEnd.Y, Colors.Red);

            // Ray internalRay; double mix;
            // Ray? exitRay = Scene.Shapes[0].RefractRay(ray, (ray * intersection.Value).End, Scene.Shapes[0].Normal(ray.End), out internalRay, out mix);
            // if (exitRay == null) {
            //     Trace.WriteLine("Wha");
            //     return;
            // }


            // screenSpace = internalRay;
            // screenSpace.Origin += new Point3D(Backbuffer.Width / 2.0, Backbuffer.Height / 2.0, 100);
            // ssEnd = screenSpace.End;
            // Trace.WriteLine(internalRay.Length);
            // Backbuffer.DrawLine((int)screenSpace.Origin.X, (int)screenSpace.Origin.Y, (int)ssEnd.X, (int)ssEnd.Y, Colors.Yellow);

            // screenSpace = exitRay.Value;
            // screenSpace *= 100;
            // screenSpace.Origin += new Point3D(Backbuffer.Width / 2.0, Backbuffer.Height / 2.0, 100);
            // ssEnd = screenSpace.End;
            // Trace.WriteLine(internalRay.Length);
            // Backbuffer.DrawLine((int)screenSpace.Origin.X, (int)screenSpace.Origin.Y, (int)ssEnd.X, (int)ssEnd.Y, Colors.SkyBlue);


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