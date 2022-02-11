using System.Windows.Media;

namespace srt {
    public struct Point3D {
        public double X, Y, Z;

        public double Length { get => System.Math.Sqrt((X * X) + (Y * Y) + (Z * Z)); }

        public Point3D(double x, double y, double z) { X = x; Y = y; Z = z; }

        public static Point3D operator +(Point3D l, Point3D r) => new Point3D(l.X + r.X, l.Y + r.Y, l.Z + r.Z);
        public static Point3D operator -(Point3D l, Point3D r) => new Point3D(l.X - r.X, l.Y - r.Y, l.Z - r.Z);
        public static Point3D operator -(Point3D l) => new Point3D(-l.X, -l.Y, -l.Z);
        public static double operator *(Point3D l, Point3D r) => (l.X * r.X) + (l.Y * r.Y) + (l.Z * r.Z);
        public static Point3D operator *(Point3D l, double r) => new Point3D(l.X * r, l.Y * r, l.Z * r);
        public static Point3D operator /(Point3D l, double r) => new Point3D(l.X / r, l.Y / r, l.Z / r);

        public override string ToString() => $"{X},{Y},{Z}";

        public Point3D Normalize() => this / System.Math.Sqrt(this * this);

        public static implicit operator Point3D(Color c) => new Point3D(c.R / 255.0, c.G / 255.0, c.B / 255.0);
        public static explicit operator Color(Point3D c) => Color.FromRgb((byte)(c.X * 255), (byte)(c.Y * 255), (byte)(c.Z * 255));

        public Point3D Lerp(Point3D second, double by, bool boundsCheck = true) {
            if (boundsCheck) {
                return by switch {
                    <= 0 => this,
                    >= 1 => second,
                    _ => new Point3D(
                        Extensions.Lerp(this.X, second.X, by),
                        Extensions.Lerp(this.Y, second.Y, by),
                        Extensions.Lerp(this.Z, second.Z, by)),
                };
            }

            return new Point3D(
                        Extensions.Lerp(this.X, second.X, by),
                        Extensions.Lerp(this.Y, second.Y, by),
                        Extensions.Lerp(this.Z, second.Z, by));
        }

        public static Point3D Lerp(Point3D first, Point3D second, double by, bool boundsCheck = true) => first.Lerp(second, by, boundsCheck);

        public static Point3D Zero = new Point3D();
    }
}
