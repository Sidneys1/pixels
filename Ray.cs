using System;

namespace srt {
    public struct Ray {
        public Point3D Origin;
        public Point3D Direction;

        public Ray(Point3D origin, Point3D direction) {
            Origin = origin;
            Direction = direction;
        }

        public Ray(double ox, double oy, double oz, double dx, double dy, double dz) {
            Origin.X = ox;
            Origin.Y = oy;
            Origin.Z = oz;
            Direction.X = dx;
            Direction.Y = dy;
            Direction.Z = dz;
        }

        public Ray Normalize() => new Ray(Origin, Direction.Normalize());

        public static Ray operator *(Ray ray, double magnitude) => new Ray(ray.Origin, ray.Direction * magnitude);

        public Point3D End { get => Origin + Direction; }
        public double Length { get => Direction.Length; }

        public override string ToString() => $"Ray({Origin} , {Direction})";
    }
}