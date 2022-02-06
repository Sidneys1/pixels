namespace srt
{
    public struct Point3D {
        public double X, Y, Z;
        public Point3D(double x, double y, double z) { X = x; Y = y; Z=z;}

        public static Point3D operator +(Point3D l, Point3D r) => new Point3D(l.X + r.X, l.Y + r.Y, l.Z + r.Z);
        public static Point3D operator -(Point3D l, Point3D r) => new Point3D(l.X - r.X, l.Y - r.Y, l.Z - r.Z);
        public static Point3D operator -(Point3D l) => new Point3D(-l.X, -l.Y, -l.Z);
        public static double operator *(Point3D l, Point3D r) => (l.X * r.X) + (l.Y * r.Y) + (l.Z * r.Z);
        public static Point3D operator *(Point3D l, double r) => new Point3D(l.X * r, l.Y * r, l.Z * r);
        public static Point3D operator /(Point3D l, double r) => new Point3D(l.X / r, l.Y / r, l.Z / r);

        public override string ToString() => $"{X},{Y},{Z}";
    }
}
