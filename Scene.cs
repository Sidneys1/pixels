using System.Collections.Generic;
using System.Windows.Media;

namespace srt
{

    public class Scene {
        public Color Ambient {get;set;} = Colors.Black;
        public Color Fog {get;set;} = Colors.White;
        public double FogIntensity {get;set;} = 0.001;
        public List<Shape> Shapes {get;} = new();
    }
}