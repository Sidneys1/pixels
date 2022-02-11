#define TRACE

// using System.Windows.Media;
namespace srt {
    using Color3 = Point3D;

    public abstract class Shape {
        public Point3D Origin { get; set; }
        public Color3 Fill { get; set; }
        public double Reflectivity { get; set; } = 0;
        public bool Refract { get; set; } = false;
        public double RefractiveCoefficient { get; set; } = 1.5;
        // public double Roughness {get;set;} = 0;
        public Shape(Point3D origin, Color3 fill) {
            Origin = origin;
            Fill = fill;
        }

        public abstract Ray Normal(Point3D intersection);

        public abstract Color3 Sample(Ray ray);
        public abstract double? Intersection(Ray ray);

        public override string ToString() => $"{this.GetType().Name} @ {Origin}";

        public Ray? RefractRay(Ray r, Point3D IntersectionPoint, Ray normal/* , out Ray internalRay, out double mix */) {
            var refractVector = Raytracer.Refract(r.Direction, normal.Direction, /* 1, */ RefractiveCoefficient)?.Normalize();
            if (refractVector == null) return null;

            // var fresnel1 = Raytracer.FresnelReflectAmount(1, RefractiveCoefficient, normal.Direction, r.Direction, this.Reflectivity);

            var internalRay = new Ray(IntersectionPoint - (normal.Direction * 0.01), -refractVector.Value).Normalize();
            if (internalRay.Length == 0) {
                // mix = 0.0;
                return null;
            }
            var exitDistance = this.Intersection(internalRay);
            if (exitDistance == null) {
                // mix = 0.0;
                return null;
            }
            internalRay *= exitDistance.Value;
            // System.Diagnostics.Debug.Assert(exitDistance.Value > 0);
            var exitPoint = internalRay.End;
            refractVector = Raytracer.Refract(internalRay.Direction, this.Normal(exitPoint).Direction, RefractiveCoefficient/* , 1 */)?.Normalize();
            // var fresnel2 = Raytracer.FresnelReflectAmount(RefractiveCoefficient, 1, normal.Direction, r.Direction, this.Reflectivity);
            // mix = (fresnel1 + fresnel2) / 2.0; // maybe?
            if (refractVector == null) return null;
            return new Ray(exitPoint, refractVector.Value);
        }
    }
}
