#define TRACE

using System;
// using System.Windows.Media;

namespace srt {
    using Color3 = Point3D;
    public class Sphere : Shape {
        public double Radius { get; set; }
        public Sphere(Point3D origin, double radius, Color3 normal) : base(origin, normal) => Radius = radius;

        double GetDistance(double x1, double y1, double x2, double y2) => Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));

        public override Color3 Sample(Ray ray) => Fill;

        private int Intersect(Ray ray, out double one, out double two) {
            var o_minus_c = ray.Origin - Origin;
            double p = ray.Direction * Origin;
            double q = (o_minus_c * o_minus_c) - (Radius * Radius);
            double discriminant = (p * p) - q;
            if (discriminant < 0) {
                one = two = 0;
                return 0;
            }

            double dRoot = Math.Sqrt(discriminant);
            one = -p - dRoot;
            two = -p + dRoot;

            return (discriminant > 1e-7) ? 2 : 1;
        }

        public override double? Intersection(Ray r) {
            double radius = this.Radius;
            Point3D oc = r.Origin - this.Origin;

            double a = r.Direction * r.Direction;
            double b = 2.0 * (oc * r.Direction);
            double c = (oc * oc) - (radius * radius);
            double discriminant = (b * b) - 4 * a * c;

            if (discriminant < 0)
                return null;

            return (-b - Math.Sqrt(discriminant)) / (2.0 * a);
        }

        public override Ray Normal(Point3D intersection) => new Ray(intersection, intersection - Origin).Normalize();
    }
}
