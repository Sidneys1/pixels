using System.Collections.Generic;
// using System.Windows.Media;

namespace srt {
    using Color3 = Point3D;
    public class Scene {
        public Point3D Ambient { get; set; } = System.Windows.Media.Colors.Black;
        public double AmbientLight { get; set; } = 0.5;
        public Point3D Fog { get; set; } = System.Windows.Media.Colors.White;
        public double FogIntensity { get; set; } = 0;
        public List<Shape> Shapes { get; } = new();
    }
}