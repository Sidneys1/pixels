#define TRACE

using System;
using System.Windows.Media;

namespace srt
{
    public class Plane : Shape {
        public Point3D Direction {get;set;} = new(0, 1, 0);
        public Ray PlaneNormal { get => new Ray(Origin, Direction); }
        public Plane(Point3D origin, Point3D direction, Color fill) : base(origin, fill) {
            Direction = direction;
        }

        public override Color Sample(Ray ray){
            var intersection = (ray * Intersection(ray).GetValueOrDefault(0)).End;
            var diffX = Origin.X - intersection.X;
            var diffZ = Origin.Z - intersection.Z;
            bool color = (diffX < 0) ^ (diffZ < 0);
            if (Math.Abs(diffZ) % 100 < 50) color = !color;
            if (Math.Abs(diffX) % 100 < 50) color = !color;
            return color ? Fill : Colors.DarkGray;
        }

        public override double? Intersection(Ray ray) {
            var denom = Direction * ray.Direction;
            if (Math.Abs(denom) > 0.0001) // your favorite epsilon
            {
                var ret = (Origin - ray.Origin) * Direction / denom;
                return ret > 0 ? ret : null;
            }
            return null;
        }

        public override Ray Normal(Point3D intersection) => PlaneNormal;
    }
}
