#define TRACE

using System;
using System.Windows.Media;

namespace srt {
    using Color3 = Point3D;
    public class Backbuffer {
        public readonly int Width, Height, BitsPerPixel, Stride;

        public byte[] Data;

        public bool Dirty { get; set; } = false;

        public Backbuffer(int width, int height) {
            Width = width;
            Height = height;
            BitsPerPixel = 3;
            Stride = BitsPerPixel * width;

            Data = new byte[Stride * height];
        }

        public void SetPixel(int x, int y, byte r, byte g, byte b) {
            Dirty = true;
            var off = (y * Stride) + (x * 3);
            lock (Data) {
                Data[off] = r;
                Data[off + 1] = g;
                Data[off + 2] = b;
            }
        }

        public void SetPixel(int x, int y, Color3 color) {
            var converted = (Color)color;
            Dirty = true;
            var off = (y * Stride) + (x * 3);
            lock (Data) {
                Data[off] = converted.R;
                Data[off + 1] = converted.G;
                Data[off + 2] = converted.B;
            }
        }

        public void DrawLine(int sx, int sy, int ex, int ey, Color3 color) {
            var converted = (Color)color;
            sx = Math.Clamp(sx, 0, Width);
            ex = Math.Clamp(ex, 0, Width);
            sy = Math.Clamp(sy, 0, Height);
            ey = Math.Clamp(ey, 0, Height);
            var xl = ex - sx;
            var yl = ey - sy;
            double step = 1 / Math.Sqrt((xl * xl) + (yl * yl));
            for (double i = 0; i < 1; i += step) {
                double ax = sx + ((double)(ex - sx) * i);
                double ay = sy + ((double)(ey - sy) * i);
                SetPixel((int)ax, (int)ay, converted.R, converted.G, converted.B);
            }
        }
    }
}
