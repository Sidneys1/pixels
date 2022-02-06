#define TRACE

using System.Windows.Media;

namespace srt {
    public abstract class Shape {
        public Point3D Origin { get; set; }
        public Color Fill { get; set; }
        public double Reflectivity { get; set; } = 0;
        // public double Roughness {get;set;} = 0;
        public Shape(Point3D origin, Color fill) {
            Origin = origin;
            Fill = fill;
        }

        public abstract Color Sample(Ray ray);
        public abstract double? Intersection(Ray ray);

        public override string ToString() => $"{this.GetType().Name} @ {Origin}";
    }
}
