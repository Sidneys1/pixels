using System.Collections.Generic;
using System.Windows.Media;

namespace srt
{

    public class Scene {
        public Color Ambient {get;set;} = Colors.Black;
        public double AmbientLight {get;set;} = 0.5;
        public Color Fog {get;set;} = Colors.White;
        public double FogIntensity {get;set;} = 0;
        public List<Shape> Shapes {get;} = new();
    }
}