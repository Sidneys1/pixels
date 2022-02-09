#define TRACE

using System;
using System.Windows.Media;

namespace srt
{
    public class Sphere : Shape {
        public double Radius {get;set;}
        public Sphere(Point3D origin, double radius, Color normal) : base(origin, normal) {
            Radius = radius;
        }

        double GetDistance(double x1, double y1, double x2, double y2) 
        {
            var x = (x1 - x2);
            x = x * x;
            var y = (y1 - y2);
            y = y * y;
            return Math.Sqrt(x + y);
        }

        public override Color Sample(Ray ray){
            return Fill;
        }

        private int Intersect(Ray ray, out double one, out double two) {
            var o_minus_c = ray.Origin - Origin;
            double p = ray.Direction * Origin;
            double q = (o_minus_c * o_minus_c) - (Radius * Radius);
            double discriminant = (p * p) - q;
            if (discriminant < 0) {
                one = 0;
                two = 0;
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
